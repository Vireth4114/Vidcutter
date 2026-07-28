using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Celeste.Mod.Vidcutter.Entities;
using Celeste.Mod.Vidcutter.Utils;
namespace Celeste.Mod.Vidcutter;

public static class VideoCreation {
    private static VidcutterModuleSettings Settings => VidcutterModule.Settings;

    public static List<VideoFile> GetAllVideos() {
        List<LoggedString> logs = LogManager.GetAllLogs();
        if (!Directory.Exists(Settings.VideoFolder) || logs.Count == 0) {
            return [];
        }

        string[] allFiles = Directory.GetFiles(Settings.VideoFolder);
        DateTime firstLog = logs[0].Time;
        
        List<VideoFile> videos = allFiles
            .Select(Path.GetFileName)
            .Select(VideoFile.Get)
            .Where(video => video.GetEndTime() >= firstLog)
            .ToList();
        
        Logger.Info("Vidcutter", $"{videos.Count}/{allFiles.Length} videos in {Settings.VideoFolder} are after start of log");
        return videos;
    }

    private static string GetVideoSuffixForDuplicates(string videoName) {
        int outputNumber = 0;
        foreach (string file in Directory.GetFiles(Settings.VideoFolder)) {
            Regex regex = new Regex(@$".*\\Vidcutter_{Regex.Escape(videoName)}_?(\d+)?\.mp4");
            Match match = regex.Match(file);
            
            if (!match.Success) continue;
            
            if (match.Groups.Count > 1 && match.Groups[1].Success) {
                outputNumber = Math.Max(int.Parse(match.Groups[1].Value), outputNumber);
            } else {
                outputNumber = Math.Max(1, outputNumber);
            }
        }
        return outputNumber == 0 ? "" : $"_{outputNumber + 1}";
    }

    public static string GetOutputVideoName(string levelName) {
        string videoName = levelName.Replace(" ", "");
        foreach (char c in Path.GetInvalidFileNameChars()) {
            videoName = videoName.Replace(c, '_');
        }
        return Path.Combine(Settings.VideoFolder, $"Vidcutter_{videoName}{GetVideoSuffixForDuplicates(videoName)}.mp4");
    }

    public static List<GameplayClip> ProcessLogs(VideoFile video) {
        return ProcessLogs(LogManager.GetAllLogs(video));
    }

    public static List<GameplayClip> ProcessLogs(LevelInAVideo levelInAVideo) {
        return ProcessLogs(LogManager.GetAllLogs(levelInAVideo.Video, levelInAVideo.Level));
    }

    public static List<GameplayClip> ProcessLogs(List<LoggedString> parsedLines) {
        List<GameplayClip> processedClips = [];
        List<LoggedString> logsForCurrentClip = [];
        for (int i = 0; i < parsedLines.Count; i++) {
            LoggedString currentLine = parsedLines[i];
            LoggedString nextLine = i < parsedLines.Count - 1 ? parsedLines[i + 1] : null;

            if (currentLine.Event == "RESTART CHAPTER") {
                processedClips.Clear();
                logsForCurrentClip.Clear();
                continue;
            }

            if (nextLine != null && nextLine.Level == currentLine.Level && nextLine.IsCleared() && nextLine.CountTowardsClear) {
                logsForCurrentClip.Add(currentLine);
                continue;
            }

            if (logsForCurrentClip.Count == 0)
                continue;
            
            LoggedString clipEnd = currentLine;

            if (clipEnd.IsCollectable() && nextLine?.Event == "DEATH") {
                clipEnd = nextLine;
            }

            if (nextLine?.BackToStartOfInterRoom() == true) {
                clipEnd = LastClearedLogToRoom(logsForCurrentClip, nextLine.Room);
                if (clipEnd == null) {
                    logsForCurrentClip.Clear();
                    continue;
                }
            }
        
            processedClips.Add(new GameplayClip(logsForCurrentClip[0], clipEnd));
            logsForCurrentClip.Clear();
        }
        return processedClips;
    }

    private static LoggedString LastClearedLogToRoom(List<LoggedString> logs, string room) {
        return logs.LastOrDefault(log => log.Room == room && log.IsCleared());
    }

    public static void ProcessLastLogFromState() {
        if (!Directory.Exists(Settings.VideoFolder)) {
            Tooltip.Show(Dialog.Clean("VIDCUTTER_TOOLTIP_VIDEO_FOLDER_NOT_FOUND"));
            return;
        }
        string lastVideo = Directory.GetFiles(Settings.VideoFolder)
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .FirstOrDefault();
        if (lastVideo == null) {
            Tooltip.Show(Dialog.Clean("VIDCUTTER_TOOLTIP_VIDEO_NOT_FOUND"));
            return;
        }
        VideoFile lastVideoFile = VideoFile.Get(lastVideo);
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
        TimeSpan startClip = stateLog.Time + TimeSpan.FromSeconds(Settings.DelayStart) - videoStartTime;
        float delay = Settings.DelayEnd;
        TimeSpan endClip = endLog.Time + TimeSpan.FromSeconds(delay) - videoStartTime;
        double clipDuration = (endClip - startClip).TotalSeconds;
        string output = GetOutputVideoName(stateLog.Level);
        
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
