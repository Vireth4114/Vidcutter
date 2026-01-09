using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Celeste.Mod.Vidcutter.Utils;

public class VideoFile {

    private readonly string filePath = "";
    private string fileName = "";
    private string extension = "";
    private DateTime? creationTime = null;
    private TimeSpan? videoDuration = null;

    public VideoFile(string filePath)
    {
        if (!File.Exists(filePath)) {
            throw new InvalidOperationException($"The file path {filePath} leads to a non-existing file. Please report this issue.");
        }
        this.filePath = filePath;
        if (string.IsNullOrEmpty(Path.GetExtension(filePath))) {
            throw new InvalidDataException($"Could not determine the file extension. Please report this issue.");
        }
        extension = Path.GetExtension(filePath)[..1].ToLower();
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
        Process process = VideoCreation.createProcess("stat",  $"-c '%w' \"{filePath}\"");
        process.Start();
        string strDate = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (!DateTime.TryParse(strDate, out DateTime parsedCreationTime)) {
            throw new InvalidDataException($"{error}");
        }
        creationTime = parsedCreationTime;
        return (DateTime)creationTime;
    }

    public TimeSpan GetVideoDuration() {
        if (videoDuration.HasValue) return (TimeSpan)videoDuration;
        videoDuration = VideoCreation.getVideoDuration(filePath);
        if (!videoDuration.HasValue) {
            throw new InvalidDataException(
                $"The duration for the video located at {filePath} couldn't be obtained. Please report this issue."
            );
        }
        return (TimeSpan)videoDuration;
    }

    public bool IsValidForClipping() {
        List<string> validFormats = ["mkv", "ts", "flv"];
        return validFormats.Contains(extension);
    }

    public bool IsFinished() {
        VideoCreation.getVideoDuration(filePath, out bool isFinished);
        return isFinished;
    }
}
