using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Celeste.Mod.UI;
using Celeste.Mod.Vidcutter.Utils;

namespace Celeste.Mod.Vidcutter.UI;

public class OuiProcessVideosProgress : OuiLoggedProgress {
    private List<LevelInAVideo> _rowsToProcess = [];
    private bool _deleteAfterProcess;

    public void Configure(List<LevelInAVideo> rowsToProcess, bool deleteAfterProcess) {
        _rowsToProcess = rowsToProcess;
        _deleteAfterProcess = deleteAfterProcess;
    }
    
    public override IEnumerator Enter(Oui from) {
        Init<OuiModOptions>(Dialog.Clean("VIDCUTTER_PROCESS_TITLE"), new Task(() => {
            int clipIdx = 1;
            int rowIdx = 1;
        
            using (StreamWriter clipsIndexWriter = new StreamWriter(FileUtils.ClipsIndexFile)) {
                foreach (LevelInAVideo levelInAVideo in _rowsToProcess) {
                    LogLine(Dialog.Clean("VIDCUTTER_PROCESSINGVIDEO") + $" {levelInAVideo.VideoName} ({levelInAVideo.Level}) ({rowIdx++}/{_rowsToProcess.Count})");
                    clipIdx = ProcessRow(levelInAVideo, clipsIndexWriter, clipIdx);
                }
            }
            
            ConcatAndClean(clipIdx);

            if (_deleteAfterProcess) {
                LogManager.DeleteLogs(_rowsToProcess);
            }
        }), 100);

        yield return base.Enter(from);
    }

    private int ProcessRow(LevelInAVideo levelInAVideo, StreamWriter clipsIndexWriter, int startIdx = 1) {
        List<GameplayClip> clips = VideoCreation.ProcessLogs(levelInAVideo);
        VideoFile video = levelInAVideo.Video;
        int clipIdx = startIdx;
        foreach (GameplayClip clip in clips) {
            Progress = 0;
            LogLine("- " + Dialog.Clean("VIDCUTTER_PROCESSINGCLIP") + $" {clipIdx - startIdx + 1}/{clips.Count}");
            Logger.Info("Vidcutter", $"Processing clip {clip}");

            string videoName = $"{clipIdx}.mp4";
                
            FFmpegUtils.CutClip(
                video, 
                clip.StartTimeWithDelay - video.GetCreationTime(), 
                clip.EndTimeWithDelay - video.GetCreationTime(),
                output: Path.Combine(FileUtils.VidcutterWorkingDirectory, videoName),
                onProgress: timeProcessed => {
                    Progress = (int)(timeProcessed.TotalSeconds / clip.Duration * 100);
                }
            );
            
            clipsIndexWriter.WriteLine($"file '{videoName}'");
            clipIdx++;
        }
        return clipIdx;
    }
    
    private void ConcatAndClean(int videoCount) {
        string output = VideoCreation.GetOutputVideoName(_rowsToProcess[0].Level);
        FFmpegUtils.ConcatenateClipsFromIndexFilePath(FileUtils.ClipsIndexFile, output);

        File.Delete(Path.Combine(FileUtils.ClipsIndexFile));
        for (int i = 1; i < videoCount; i++) {
            File.Delete(Path.Combine(FileUtils.VidcutterWorkingDirectory, $"{i}.mp4"));
        }
    }
}