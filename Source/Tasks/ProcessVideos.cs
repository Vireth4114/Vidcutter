using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Celeste.Mod.Vidcutter.Models;
using Celeste.Mod.Vidcutter.Progress;
using Celeste.Mod.Vidcutter.Services;
using Celeste.Mod.Vidcutter.Services.GameplayClips;
using Celeste.Mod.Vidcutter.Services.Logs;
using Celeste.Mod.Vidcutter.Utils;
using static Celeste.Mod.Vidcutter.Utils.FileConstants;

namespace Celeste.Mod.Vidcutter.Tasks;

public class ProcessVideos(IProgress progress, FFmpegService ffmpegService, ClipDelays delays, string outputFolder) {
    private static readonly string ClipsIndexFile = Path.Combine(VidcutterWorkingDirectory, "videos.txt");
    private readonly ClipProcessor _clipProcessor = new(delays);
    private VideoFileRepository VideoFileRepository => new(ffmpegService);
    

    public void Execute(List<LevelInAVideo> rows, Action onComplete = null) {
        progress.Task = new Task(() => ProcessVideoTask(rows));
        progress.OnComplete += onComplete;
        progress.Start();
    }

    private void ProcessVideoTask(List<LevelInAVideo> rows) {
        try {
            int clipIdx = 1;
            int rowIdx = 1;

            using (StreamWriter clipsIndexWriter = new(ClipsIndexFile)) {
                foreach (LevelInAVideo levelInAVideo in rows) {
                    progress.AddLine(Dialog.Clean("VIDCUTTER_PROCESSINGVIDEO") + $" {levelInAVideo.VideoName} ({levelInAVideo.Level}) ({rowIdx++}/{rows.Count})");
                    clipIdx = ProcessRow(levelInAVideo, clipsIndexWriter, clipIdx);
                }
            }

            string output = VideoUtils.GetOutputVideoName(outputFolder, rows[0].Level);
            ffmpegService.ConcatenateClipsFromIndexFilePath(ClipsIndexFile, output);
        } finally {
            CleanCreatedFiles();
        }
    }

    private int ProcessRow(LevelInAVideo levelInAVideo, StreamWriter clipsIndexWriter, int startIdx = 1) {
        List<LoggedString> logsForRow = LogService.GetAllLogs(
            VideoFileRepository.Get(levelInAVideo.VideoPath),
            levelInAVideo.Level
        );
        List<GameplayClip> clips = _clipProcessor.GetClipsFromLogs(logsForRow).FindAll(clip => clip.Duration > 0.2);
        VideoFile video = VideoFileRepository.Get(levelInAVideo.VideoPath);
        int clipIdx = startIdx;
        foreach (GameplayClip clip in clips) {
            progress.Progress = 0;
            progress.AddLine("- " + Dialog.Clean("VIDCUTTER_PROCESSINGCLIP") + $" {clipIdx - startIdx + 1}/{clips.Count}");
            Logger.Info("Vidcutter", $"Processing clip {clip}");

            string videoName = $"{clipIdx}.mp4";
                
            ffmpegService.CutClip(
                video.FilePath, 
                clip.StartTimeWithDelay - video.CreationTime, 
                clip.EndTimeWithDelay - video.CreationTime,
                output: Path.Combine(VidcutterWorkingDirectory, videoName),
                newProgress => progress.Progress = newProgress
            );
            
            clipsIndexWriter.WriteLine($"file '{videoName}'");
            clipIdx++;
        }
        return clipIdx;
    }

    private void CleanCreatedFiles() {
        File.Delete(Path.Combine(ClipsIndexFile));
        Directory.GetFiles(VidcutterWorkingDirectory, "*.mp4").ToList().ForEach(File.Delete);
    }
}