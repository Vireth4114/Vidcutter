using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Celeste.Mod.Vidcutter.Models;
using Celeste.Mod.Vidcutter.Services.Logs;

namespace Celeste.Mod.Vidcutter.Services;

public class VideoFileRepository(VideoDurationProvider videoDurationProvider) {
    private readonly Dictionary<string, VideoFile> _videoFileCache = new();
    private readonly Dictionary<string, bool> _canBeProcessed = new();

    private TimeSpan? GetVideoDuration(string filePath) {
        bool success = videoDurationProvider.TryGetVideoDuration(filePath, out TimeSpan duration, out bool canBeProcessed);
        
        _canBeProcessed[filePath] = canBeProcessed;

        return success ? duration : null;
    }
    
    public VideoFile Get(string filePath) {
        if (_videoFileCache.TryGetValue(filePath, out VideoFile cachedVideoFile))
            return cachedVideoFile;

        VideoFile videoFile = new(
            filePath,
            GetCreationTime(filePath),
            GetEndTime(filePath),
            _canBeProcessed.GetValueOrDefault(filePath, true)
        );
        if (!videoFile.IsStillWriting()) {
            _videoFileCache[filePath] = videoFile;
        }

        return videoFile;
    }
    
    public List<VideoFile> GetAllVideos(string folder) {
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

    private DateTime GetCreationTime(string filePath) {
        if (OperatingSystem.IsWindows())
            return File.GetCreationTime(filePath);
        
        if (GetVideoDuration(filePath) is { } duration)
            return File.GetLastWriteTime(filePath) - duration;
        
        if (DateTime.TryParse(Path.GetFileName(filePath), out DateTime fileNameDate))
            return fileNameDate; 
        
        throw new InvalidDataException( 
            $"The creation time for the video located at {filePath} couldn't be obtained."
        );
    }

    private DateTime GetEndTime(string filePath) {
        if (OperatingSystem.IsWindows() && GetVideoDuration(filePath) is { } duration)
            return File.GetCreationTime(filePath) + duration;
        
        return File.GetLastWriteTime(filePath);
    }
    
}