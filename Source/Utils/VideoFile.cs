using System;
using System.IO;

namespace Celeste.Mod.Vidcutter.Utils;

public class VideoFile {
    public string FilePath { get; }
    public string FileName { get; }
    public bool CanBeProcessed { get; private set; }

    private TimeSpan? _durationFromMetadata;
    
    private DateTime? _cachedCreationTime;
    private DateTime? _cachedEndTime;

    public VideoFile(string filePath) {
        if (!File.Exists(filePath)) {
            throw new InvalidOperationException($"The file path {filePath} leads to a non-existing file. Please report this issue.");
        }
        
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        CanBeProcessed = true;
        TryGetVideoDurationFromMetadata(out TimeSpan _); // Cache duration at initialization as it is always used, to check immediately if the file can be processed
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
        else if (IsStillWriting())
            _cachedEndTime = File.GetLastWriteTime(FilePath);
        else
            return File.GetLastWriteTime(FilePath);

        return _cachedEndTime.Value;
    }

    private bool TryGetVideoDurationFromMetadata(out TimeSpan duration) {
        if (_durationFromMetadata.HasValue) {
            duration = _durationFromMetadata.Value;
            return true;
        }

        if (VidcutterModule.DurationCache.TryGetValue(FilePath, out duration)) {
            _durationFromMetadata = duration;
            return true;
        }
        
        string strDuration;
        try {
            strDuration = FFmpegUtils.GetDurationString(FilePath);
        } catch (InvalidOperationException) {
            CanBeProcessed = false;
            return false;
        }

        if (!double.TryParse(strDuration, out double durationDouble) || durationDouble <= 0) {
            return false;
        }
        
        duration = TimeSpan.FromSeconds(durationDouble);
        VidcutterModule.writeCache(FilePath, duration);
        _durationFromMetadata = duration;
        return true;
    }

    public bool IsStillWriting() {
        return DateTime.Now - File.GetLastWriteTime(FilePath) < TimeSpan.FromSeconds(5);
    }

    public override string ToString() {
        return $"Video {FileName} ({FilePath}) started at {GetCreationTime()} and ending at {GetEndTime()}";
    }
}
