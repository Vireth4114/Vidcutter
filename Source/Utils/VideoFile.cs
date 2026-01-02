using System;
using System.IO;

namespace Celeste.Mod.Vidcutter.Utils;

public class VideoFile {

    private string filePath = "";
    private string fileName = "";
    private DateTime? creationTime = null;
    private TimeSpan? videoDuration = null;

    public VideoFile(string filePath)
    {
        if (!File.Exists(filePath)) {
            Logger.Error("Vidcutter", $"Tried to access a file that doesn't exist, path given: {filePath}");
            throw new InvalidOperationException($"The file path {filePath} leads to a non-existing file. Please report this issue.");
        }
        this.filePath = filePath;
    }

    public override string ToString() {
        return $"Video {GetFileName()} ({filePath}) started at {GetCreationTime()} and has duration {GetVideoDuration()}";
    }

    public string GetFilePath() {
        return filePath;
    }

    public string GetFileName() {
        if (fileName != "") return fileName;
        fileName = Path.GetFileName(filePath);
        return fileName;
    }

    public DateTime GetCreationTime() {
        if (creationTime.HasValue) return (DateTime)creationTime;
        creationTime = File.GetCreationTime(filePath);
        if (OperatingSystem.IsWindows()) return (DateTime)creationTime;
        creationTime -= GetVideoDuration();
        return (DateTime)creationTime;
    }

    public TimeSpan GetVideoDuration() {
        if (videoDuration.HasValue) return (TimeSpan)videoDuration;
        videoDuration = VideoCreation.getVideoDuration(filePath);
        if (!videoDuration.HasValue) {
            Logger.Error("Vidcutter", $"Couldn't get the video duration for the video located at {filePath}");
            throw new InvalidDataException(
                $"The duration for the video located at {filePath} couldn't be obtained. Please report this issue."
            );
        }
        return (TimeSpan)videoDuration;
    }
}
