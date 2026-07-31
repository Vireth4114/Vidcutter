using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Celeste.Mod.Vidcutter.Entities;
using Celeste.Mod.Vidcutter.Models;

namespace Celeste.Mod.Vidcutter.Utils;

public static class VideoManager {
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
                processedClips = processedClips.Where(clip => clip.Level != currentLine.Level).ToList();
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

    public static void ProcessLastLogFromStateWithTooltip() {
        if (!Directory.Exists(Settings.VideoFolder)) {
            Tooltip.Show(Dialog.Clean("VIDCUTTER_TOOLTIP_VIDEO_FOLDER_NOT_FOUND"));
            return;
        }
        
        string lastVideo = Directory.GetFiles(Settings.VideoFolder)
            .OrderByDescending(File.GetLastWriteTime)
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
        
        GameplayClip clip = new(stateLog, logs.Last());
        
        TooltipWithProgress progress = TooltipWithProgress.Show(Dialog.Clean("VIDCUTTER_TOOLTIP_PROCESSING_VIDEO"));
        
        if (lastVideoFile.IsStillWriting()) {
            progress.AddLoadingDelay(5f, () => ProcessClipInTooltip(progress, lastVideoFile, clip));
        } else {
            ProcessClipInTooltip(progress, lastVideoFile, clip);
        }
    }

    private static void ProcessClipInTooltip(TooltipWithProgress progress, VideoFile video, GameplayClip clip) {
        string output = GetOutputVideoName(clip.Level);
        
        FFmpegUtils.NonBlockingCutClip(
            video, 
            clip.StartTimeWithDelay - video.GetCreationTime(), 
            clip.EndTimeWithDelay - video.GetCreationTime(),
            output,
            onProgress: timeProcessed => {
                progress.Progress = (float) (timeProcessed.TotalSeconds / clip.Duration);
            }
        ).Exited += (_, _) => {
            progress.Progress = 1f;
            Tooltip.Show(output + " " + Dialog.Clean("VIDCUTTER_TOOLTIP_PROCESSED_VIDEO"), 3f);
        };
    }
}
