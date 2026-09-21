using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Celeste.Mod.Vidcutter.Models;
using Celeste.Mod.Vidcutter.Utils.Logs;
using static Celeste.Mod.Vidcutter.Utils.FileConstants;

namespace Celeste.Mod.Vidcutter.Utils;

public static class VideoFileRepository {
    private static readonly string DurationCacheFile = Path.Combine(VidcutterWorkingDirectory, "durationCache.txt");
    private static readonly Dictionary<string, TimeSpan> DurationCache = new();
    private static readonly Dictionary<string, VideoFile> VideoFileCache = new();
    private static readonly Dictionary<string, bool> CanBeProcessed = new();

    private static bool _isDurationCacheLoaded;
    
    private static void LoadDurationCache() {
        if (!File.Exists(DurationCacheFile))
            return;

        DurationCache.Clear();
        string[] lines = File.ReadAllLines(DurationCacheFile);
        foreach (string line in lines) {
            string[] parts = line.Split(" | ");
            if (parts.Length == 2) {
                DurationCache[parts[0]] = TimeSpan.Parse(parts[1]);
            }
        }
        _isDurationCacheLoaded = true;
    }

    public static VideoFile Get(string filePath) {
        if (!_isDurationCacheLoaded)
            LoadDurationCache();
        
        if (VideoFileCache.TryGetValue(filePath, out VideoFile cachedVideoFile))
            return cachedVideoFile;

        VideoFile videoFile = new VideoFile(
            filePath,
            GetCreationTime(filePath),
            GetEndTime(filePath),
            CanBeProcessed.GetValueOrDefault(filePath, true)
        );
        if (!videoFile.IsStillWriting()) {
            VideoFileCache[filePath] = videoFile;
        }

        return videoFile;
    }
    
    public static List<VideoFile> GetAllVideos(string folder) {
        List<LoggedString> logs = LogService.GetAllLogs();
        if (!Directory.Exists(folder) || logs.Count == 0) {
            return [];
        }

        string[] allFiles = Directory.GetFiles(folder);
        DateTime firstLog = logs[0].Time;
        
        List<VideoFile> videos = allFiles
            .Select(Path.GetFileName)
            .Select(v => Path.Combine(folder, Path.GetFileName(v)))
            .Select(Get)
            .Where(video => video.EndTime >= firstLog)
            .ToList();
        
        Logger.Info("Vidcutter", $"{videos.Count}/{allFiles.Length} videos in {folder} are after start of log");
        return videos;
    }

    private static DateTime GetCreationTime(string filePath) {
        if (OperatingSystem.IsWindows())
            return File.GetCreationTime(filePath);
        
        if (TryGetVideoDurationFromMetadata(filePath, out TimeSpan duration))
            return File.GetLastWriteTime(filePath) - duration;
        
        if (DateTime.TryParse(Path.GetFileName(filePath), out DateTime fileNameDate))
            return fileNameDate; 
        
        throw new InvalidDataException( 
            $"The creation time for the video located at {filePath} couldn't be obtained."
        );
    }

    private static DateTime GetEndTime(string filePath) {
        if (OperatingSystem.IsWindows() && TryGetVideoDurationFromMetadata(filePath, out TimeSpan duration))
            return File.GetCreationTime(filePath) + duration;
        
        return File.GetLastWriteTime(filePath);
    }
    
    private static bool TryGetVideoDurationFromMetadata(string filePath, out TimeSpan duration) {
        if (DurationCache.TryGetValue(filePath, out duration)) {
            return true;
        }
        
        string strDuration;
        try {
            strDuration = FFmpegUtils.GetDurationString(filePath);
        } catch (InvalidOperationException) {
            // If ffprobe throw an exception on the video, ffmpeg can't process it either
            CanBeProcessed[filePath] = false;
            return false;
        }

        if (!double.TryParse(strDuration, out double durationDouble) || durationDouble <= 0) {
            // ffprobe can process the video but doesn't know its duration, may be a running mkv or missing metadata
            return false;
        }
        
        duration = TimeSpan.FromSeconds(durationDouble);
        CanBeProcessed[filePath] = true;
        WriteCacheInFile(filePath, duration);
        return true;
    }


    private static void WriteCacheInFile(string video, TimeSpan duration) {
        if (!DurationCache.TryAdd(video, duration))
            return;
        
        using StreamWriter writer = new(DurationCacheFile, false);
        foreach (KeyValuePair<string, TimeSpan> entry in DurationCache)
            writer.WriteLine($"{entry.Key} | {entry.Value}");
    }
}