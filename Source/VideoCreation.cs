using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Celeste.Mod.UI;
using Celeste.Mod.Vidcutter.Utils;
namespace Celeste.Mod.Vidcutter;

public class VideoCreation(OuiVidcutterProgress progress = null) {
    public List<ProcessedVideo> videos = [];
    public OuiVidcutterProgress progress = progress;

    public static List<VideoFile> GetAllVideos() {
        List<LoggedString> logs = LogManager.GetAllLogs();
        if (logs.Count == 0) {
            return [];
        }
        DateTime firstLog = logs[0].Time;
        List<VideoFile> videos = [];
        if (!Directory.Exists(VidcutterModule.Settings.VideoFolder)) {
            return videos;
        }
        string[] allVideos = Directory.GetFiles(VidcutterModule.Settings.VideoFolder);
        foreach (string videoPath in allVideos) {
            VideoFile video = new(videoPath);
            DateTime videoTime = video.GetEndTime();
            if (videoTime >= firstLog) {
                videos.Add(video);
            }
        }
        Logger.Info("Vidcutter", $"{videos.Count}/{allVideos.Length} videos in {VidcutterModule.Settings.VideoFolder} are after start of log");
        return videos;
    }

    public void ProcessVideosProgress(bool withDelete = false) {
        progress.Init<OuiModOptions>(Dialog.Clean("VIDCUTTER_PROCESS_TITLE"), new Task(() => {
            int idx = 1;
            int videoIdx = 1;
            foreach (ProcessedVideo video in videos) {
                progress.LogLine(Dialog.Clean("VIDCUTTER_PROCESSINGVIDEO") + $" {video.Video} ({videoIdx++}/{videos.Count})");
                idx = ProcessVideo(video, idx);
            }
            ConcatAndClean(idx);
            if (withDelete) {
                LogManager.deleteLogs(videos);
            }
        }), 100);
    }

    public int ProcessVideo(ProcessedVideo processedVideo, int startIdx = 1) {
        VideoFile video = new (Path.Combine(VidcutterModule.Settings.VideoFolder, processedVideo.Video));
        DateTime startVideo = video.GetCreationTime();
        DateTime endVideo = video.GetEndTime();
        List<LoggedString[]> processed = ProcessLogs(startVideo, endVideo, processedVideo.Level);
        
        StreamWriter listVideos = new StreamWriter(Path.Combine("./VidCutter/", Path.Combine("videos.txt")), true);
        int videoIdx = startIdx;
        foreach (LoggedString[] line in processed) {
            progress.Progress = 0;
            TimeSpan startClip;
            if (line[0] == null) {
                startClip = TimeSpan.Zero;
            } else {
                startClip =  line[0].Time + TimeSpan.FromSeconds(VidcutterModule.Settings.DelayStart) - startVideo;
            }
            float delay = VidcutterModule.Settings.DelayEnd;
            if (line[1].Event == "LEVEL COMPLETE") {
                delay = VidcutterModule.Settings.DelayComplete;
            }
            TimeSpan endClip = line[1].Time + TimeSpan.FromSeconds(delay) - startVideo;
            double clipDuration = (endClip - startClip).TotalSeconds;
            progress.LogLine("- " + Dialog.Clean("VIDCUTTER_PROCESSINGCLIP") + $" {videoIdx - startIdx + 1}/{processed.Count}");
            FFmpegUtils.CutClip(
                video, 
                startClip, 
                endClip,
                output: $"./VidCutter/{videoIdx}.mp4",
                onProgress: timeProcessed => {
                    progress.Progress = (int)(timeProcessed.TotalSeconds / clipDuration * 100);
                }
            );
            listVideos.WriteLine($"file '{videoIdx}.mp4'");
            videoIdx++;
        }
        listVideos.Close();
        return videoIdx;
    }

    public void ConcatAndClean(int videoCount) {
        string output = getOutputVideoName(videos[0].Level);
        Logger.Info("Vidcutter", $"Concatenating {videoCount} videos into {output}");
        FFmpegUtils.ConcatenateClipsFromIndexFilePath("./VidCutter/videos.txt", output);
        Logger.Info("Vidcutter", $"Concatenation done, saved at {output}. Starting cleaning process.");

        File.Delete("./VidCutter/videos.txt");
        for (int i = 1; i < videoCount; i++) {
            File.Delete($"./VidCutter/{i}.mp4");
        }
        Logger.Info("Vidcutter", "Cleaning process has ended correctly.");
    }

    public static string getOutputVideoName(string levelName) {
        string videoName = levelName.Replace(" ", "");
        int outputNumber = 0;
        foreach (string file in Directory.GetFiles(VidcutterModule.Settings.VideoFolder)) {
            Regex regex = new Regex(@$".*\\Vidcutter_{Regex.Escape(videoName)}_?(\d+)?\.mp4");
            Match match = regex.Match(file);
            Logger.Info("Vidcutter", $"Checking existing file {file} against pattern {regex}: Match success: {match.Success}");
            if (match.Success) {
                if (match.Groups.Count > 1 && match.Groups[1].Success) {
                    outputNumber = Math.Max(int.Parse(match.Groups[1].Value), outputNumber);
                } else {
                    outputNumber = Math.Max(1, outputNumber);
                }
            }
        }
        foreach (char c in Path.GetInvalidFileNameChars()) {
            videoName = videoName.Replace(c, '_');
        }
        string output = Path.Combine(VidcutterModule.Settings.VideoFolder, $"Vidcutter_{videoName}");
        if (outputNumber > 0) {
            output += $"_{outputNumber + 1}";
        }
        output += ".mp4";
        return output;
    }

    public static List<LoggedString[]> ProcessLogs(VideoFile video) {
        return ProcessLogs(LogManager.GetAllLogs(video));
    }

    public static List<LoggedString[]> ProcessLogs(DateTime startTime, DateTime endTime, string level = null) {
        return ProcessLogs(LogManager.GetAllLogs(startTime, endTime, level));
    }

    public static List<LoggedString[]> ProcessLogs(List<LoggedString> parsedLines) {
        List<LoggedString[]> processed = new List<LoggedString[]>();
        List<LoggedString> currentClip = new List<LoggedString>();
        for (int i = 0; i < parsedLines.Count; i++) {
            LoggedString previousLine = i > 0 ? parsedLines[i - 1] : null;
            LoggedString currentLine = parsedLines[i];
            LoggedString nextline = i < parsedLines.Count - 1 ? parsedLines[i + 1] : null;

            if (currentLine.Event == "RESTART CHAPTER") {
                processed.Clear();
            }
            
            if (!currentLine.isCleared() || !currentLine.CountTowardsClear) {
                currentClip.Clear();
                continue;
            }

            currentClip.Add(previousLine);
            
            if (nextline?.isCleared() != true) {
                LoggedString clipEnd = currentLine;
                
                if (nextline?.Event == "INTER ROOM PASSED") {
                    currentClip.Add(currentLine);
                    clipEnd = currentClip.LastOrDefault(log => log.Room == nextline.Room && log.isCleared());
                }
                
                if (clipEnd != null) {
                    processed.Add([currentClip[0], clipEnd]);
                }
            }
        }
        return processed;
    }

    public static void ProcessLastLogFromState() {
        if (!Directory.Exists(VidcutterModule.Settings.VideoFolder)) {
            Tooltip.Show(Dialog.Clean("VIDCUTTER_TOOLTIP_VIDEO_FOLDER_NOT_FOUND"));
            return;
        }
        string lastVideo = Directory.GetFiles(VidcutterModule.Settings.VideoFolder)
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .FirstOrDefault();
        if (lastVideo == null) {
            Tooltip.Show(Dialog.Clean("VIDCUTTER_TOOLTIP_VIDEO_NOT_FOUND"));
            return;
        }
        VideoFile lastVideoFile = new(lastVideo);
        if (!lastVideoFile.CanBeProcessed) {
            Tooltip.Show(Dialog.Clean("VIDCUTTER_TOOLTIP_INVALID_FORMAT_FOR_CLIPPING"));
            return;
        }
        List<LoggedString> logs = LogManager.GetAllLogs(lastVideoFile);
        LoggedString stateLog = logs.LastOrDefault(log => log.Event.Contains("STATE"));
        if (stateLog == null) {
            Tooltip.Show(Dialog.Clean("VIDCUTTER_TOOLTIP_STATE_NOT_FOUND"));
            return;
        }
        LoggedString endLog = logs.LastOrDefault();
        
        TooltipWithProgress progress = TooltipWithProgress.Show(Dialog.Clean("VIDCUTTER_TOOLTIP_PROCESSING_VIDEO"));
        
        void process() => ProcessLastLogFromState(progress, lastVideoFile, stateLog, endLog);
        if (lastVideoFile.IsStillWriting()) {
            progress.AddLoadingDelay(5f, process);
        } else {
            process();
        }
    }

    public static void ProcessLastLogFromState(TooltipWithProgress progress, VideoFile video, LoggedString stateLog, LoggedString endLog) {
        DateTime videoStartTime = video.GetCreationTime();
        TimeSpan startClip = stateLog.Time + TimeSpan.FromSeconds(VidcutterModule.Settings.DelayStart) - videoStartTime;
        float delay = VidcutterModule.Settings.DelayEnd;
        TimeSpan endClip = endLog.Time + TimeSpan.FromSeconds(delay) - videoStartTime;
        double clipDuration = (endClip - startClip).TotalSeconds;
        string output = getOutputVideoName(stateLog.Level);
        
        FFmpegUtils.NonBlockingCutClip(
            video, 
            startClip, 
            endClip,
            output,
            onProgress: timeProcessed => {
                progress.progress = (float) (timeProcessed.TotalSeconds / clipDuration);
            }
        ).Exited += (_, _) => {
            progress.progress = 1f;
            Tooltip.Show(output + " " + Dialog.Clean("VIDCUTTER_TOOLTIP_PROCESSED_VIDEO"), 3f);
        };
    }
}
