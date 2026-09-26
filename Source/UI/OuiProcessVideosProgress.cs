using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Celeste.Mod.UI;
using Celeste.Mod.Vidcutter.Models;
using Celeste.Mod.Vidcutter.Utils;
using Celeste.Mod.Vidcutter.Utils.Logs;
using static Celeste.Mod.Vidcutter.Utils.FileConstants;

namespace Celeste.Mod.Vidcutter.UI;

public class OuiProcessVideosProgress : OuiLoggedProgress {
    private static readonly string ClipsIndexFile = Path.Combine(VidcutterWorkingDirectory, "videos.txt");
    private readonly ClipProcessor _clipProcessor = new(ClipDelays.FromSettings(VidcutterModule.Settings));
    private string VideoFolder => VidcutterModule.Settings.VideoFolder;
    
    private List<LevelInAVideo> _rowsToProcess = [];
    private bool _deleteAfterProcess;
    private FFmpegService _ffmpegService;
    private VideoFileRepository _videoFileRepository;

    public void Configure(List<LevelInAVideo> rowsToProcess, bool deleteAfterProcess, FFmpegService ffmpegService) {
        _rowsToProcess = rowsToProcess;
        _deleteAfterProcess = deleteAfterProcess;
        _ffmpegService = ffmpegService;
    }
    
    public override IEnumerator Enter(Oui from) {
        _videoFileRepository = new VideoFileRepository(_ffmpegService);
        Init<OuiModOptions>(Dialog.Clean("VIDCUTTER_PROCESS_TITLE"), new Task(() => {
            try {
                int clipIdx = 1;
                int rowIdx = 1;
            
                using (StreamWriter clipsIndexWriter = new StreamWriter(ClipsIndexFile)) {
                    foreach (LevelInAVideo levelInAVideo in _rowsToProcess) {
                        LogLine(Dialog.Clean("VIDCUTTER_PROCESSINGVIDEO") + $" {levelInAVideo.VideoName} ({levelInAVideo.Level}) ({rowIdx++}/{_rowsToProcess.Count})");
                        clipIdx = ProcessRow(levelInAVideo, clipsIndexWriter, clipIdx);
                    }
                }
                
                string output = VideoUtils.GetOutputVideoName(VideoFolder, _rowsToProcess[0].Level);
                _ffmpegService.ConcatenateClipsFromIndexFilePath(ClipsIndexFile, output);

                if (_deleteAfterProcess) {
                    LogService.DeleteLogs(_rowsToProcess);
                }
            } finally {
                Clean();
            }
        }), 100);

        yield return base.Enter(from);
    }

    private int ProcessRow(LevelInAVideo levelInAVideo, StreamWriter clipsIndexWriter, int startIdx = 1) {
        List<LoggedString> logsForRow = LogService.GetAllLogs(
            _videoFileRepository.Get(levelInAVideo.VideoPath),
            levelInAVideo.Level
        );
        List<GameplayClip> clips = _clipProcessor.GetClipsFromLogs(logsForRow).FindAll(clip => clip.Duration > 0.2);
        VideoFile video = _videoFileRepository.Get(levelInAVideo.VideoPath);
        int clipIdx = startIdx;
        foreach (GameplayClip clip in clips) {
            Progress = 0;
            LogLine("- " + Dialog.Clean("VIDCUTTER_PROCESSINGCLIP") + $" {clipIdx - startIdx + 1}/{clips.Count}");
            Logger.Info("Vidcutter", $"Processing clip {clip}");

            string videoName = $"{clipIdx}.mp4";
                
            _ffmpegService.CutClip(
                video.FilePath, 
                clip.StartTimeWithDelay - video.CreationTime, 
                clip.EndTimeWithDelay - video.CreationTime,
                output: Path.Combine(VidcutterWorkingDirectory, videoName),
                onProgress: timeProcessed => {
                    Progress = (int)(timeProcessed.TotalSeconds / clip.Duration * 100);
                }
            );
            
            clipsIndexWriter.WriteLine($"file '{videoName}'");
            clipIdx++;
        }
        return clipIdx;
    }

    private void Clean() {
        File.Delete(Path.Combine(ClipsIndexFile));
        Directory.GetFiles(VidcutterWorkingDirectory, "*.mp4").ToList().ForEach(File.Delete);
    }
}