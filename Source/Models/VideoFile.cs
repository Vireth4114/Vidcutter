using System;
using System.Collections.Generic;
using System.IO;
using Celeste.Mod.Vidcutter.Utils;

namespace Celeste.Mod.Vidcutter.Models;

public class VideoFile {
    private static VidcutterModuleSettings Settings => VidcutterModule.Settings;
    private static readonly Dictionary<string, TimeSpan> DurationCache = new();
    private static readonly Dictionary<string, VideoFile> VideoFileCache = new();

    public string FilePath { get; }
    public string FileName { get; }
    public bool CanBeProcessed { get; private set; }

    private TimeSpan? _durationFromMetadata;
    
    private DateTime? _cachedCreationTime;
    private DateTime? _cachedEndTime;

    public static void LoadDurationCache() {
        if (!File.Exists(FileUtils.DurationCacheFile))
            return;

        DurationCache.Clear();
        string[] lines = File.ReadAllLines(FileUtils.DurationCacheFile);
        foreach (string line in lines) {
            string[] parts = line.Split(" | ");
            if (parts.Length == 2) {
                DurationCache[parts[0]] = TimeSpan.Parse(parts[1]);
            }
        }
    }

    private VideoFile(string filePath) {
        if (!File.Exists(filePath)) {
            throw new InvalidOperationException($"The file path {filePath} leads to a non-existing file. Please report this issue.");
        }
        
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        CanBeProcessed = true;
        TryGetVideoDurationFromMetadata(out TimeSpan _); // Cache duration at initialization as it is always used, to check immediately if the file can be processed
    }

    public static VideoFile Get(string fileName) {
        string filePath = Path.Combine(Settings.VideoFolder, fileName);

        if (VideoFileCache.TryGetValue(filePath, out VideoFile cachedVideoFile))
            return cachedVideoFile;

        return VideoFileCache[filePath] = new VideoFile(filePath);
    }

    public DateTime GetCreationTime() {
        if (_cachedCreationTime.HasValue)
            return _cachedCreationTime.Value;
        
        if (OperatingSystem.IsWindows())
            _cachedCreationTime = File.GetCreationTime(FilePath);
        else if (TryGetVideoDurationFromMetadata(out TimeSpan duration))
            _cachedCreationTime = File.GetLastWriteTime(FilePath) - duration;
        else if (DateTime.TryParse(FileName, out DateTime fileNameDate))
            _cachedCreationTime = fileNameDate; 
        else
            throw new InvalidDataException( 
                $"The creation time for the video located at {FilePath} couldn't be obtained."
            );
        
        return _cachedCreationTime.Value;
    }

    public DateTime GetEndTime() {
        if (_cachedEndTime.HasValue)
            return _cachedEndTime.Value;

        if (OperatingSystem.IsWindows() && TryGetVideoDurationFromMetadata(out TimeSpan duration))
            _cachedEndTime = File.GetCreationTime(FilePath) + duration;
        else
            return File.GetLastWriteTime(FilePath);

        return _cachedEndTime.Value;
    }

    private bool TryGetVideoDurationFromMetadata(out TimeSpan duration) {
        if (_durationFromMetadata.HasValue) {
            duration = _durationFromMetadata.Value;
            return true;
        }

        if (DurationCache.TryGetValue(FilePath, out duration)) {
            _durationFromMetadata = duration;
            return true;
        }
        
        string strDuration;
        try {
            Logger.Info("Vidcutter", "a");
            strDuration = FFmpegUtils.GetDurationString(FilePath);
        } catch (InvalidOperationException) {
            Logger.Info("Vidcutter", "b");
            // If ffprobe throw an exception on the video, ffmpeg can't process it either
            CanBeProcessed = false;
            return false;
        }

        Logger.Info("Vidcutter", "c");
        if (!double.TryParse(strDuration, out double durationDouble) || durationDouble <= 0) {
            Logger.Info("Vidcutter", "d");
            // ffprobe can process the video but doesn't know its duration, may be a running mkv or missing metadata
            return false;
        }
        Logger.Info("Vidcutter", "e");
        
        duration = TimeSpan.FromSeconds(durationDouble);
        CanBeProcessed = true;
        WriteCacheInFile(FilePath, duration);
        _durationFromMetadata = duration;
        return true;
    }

    private static void WriteCacheInFile(string video, TimeSpan duration) {
        if (!DurationCache.TryAdd(video, duration))
            return;
        
        using StreamWriter writer = new StreamWriter(FileUtils.DurationCacheFile, false);
        foreach (KeyValuePair<string, TimeSpan> entry in DurationCache)
            writer.WriteLine($"{entry.Key} | {entry.Value}");
    }

    public bool IsStillWriting() {
        return DateTime.Now - File.GetLastWriteTime(FilePath) < TimeSpan.FromSeconds(5);
    }

    public override string ToString() {
        return $"Video {FileName} ({FilePath}) started at {GetCreationTime()} and ending at {GetEndTime()}";
    }
}
