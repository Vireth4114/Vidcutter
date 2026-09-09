using System;
using System.IO;

namespace Celeste.Mod.Vidcutter.Models;

public class VideoFile {
    public string FilePath { get; }
    public string FileName { get; }
    public bool CanBeProcessed { get; }
    
    public DateTime CreationTime { get; }
    public DateTime EndTime { get; }

    public VideoFile(string filePath, DateTime creationTime, DateTime endTime, bool canBeProcessed = true) {
        if (!File.Exists(filePath)) {
            throw new InvalidOperationException($"The file path {filePath} leads to a non-existing file. Please report this issue.");
        }
        
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        CanBeProcessed = canBeProcessed;
        CreationTime = creationTime;
        EndTime = endTime;
    }

    public bool IsDuringVideo(DateTime dateTime) {
        return CreationTime <= dateTime && dateTime <= EndTime;
    }

    public bool IsStillWriting() {
        return DateTime.Now - File.GetLastWriteTime(FilePath) < TimeSpan.FromSeconds(5);
    }

    public override string ToString() {
        return $"Video {FileName} ({FilePath}) started at {CreationTime} and ending at {EndTime}";
    }
}
