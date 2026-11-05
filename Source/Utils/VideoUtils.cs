using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Celeste.Mod.Vidcutter.Utils;

public static class VideoUtils {
    private static readonly HashSet<string> VideoFileExtensions = new(StringComparer.OrdinalIgnoreCase) {
        "mp4", "mov", "avi", "mkv", "wmv",
        "flv", "webm", "mpeg", "mpg", "m4v",
        "3gp", "ts", "mts", "m2ts", "vob", "gif"
    };

    private static string GetVideoSuffixForDuplicates(string videoFolder, string videoName) {
        int outputNumber = 0;
        foreach (string file in Directory.GetFiles(videoFolder)) {
            Regex regex = new(@$".*\\Vidcutter_{Regex.Escape(videoName)}_?(\d+)?\.mp4");
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

    public static string GetOutputVideoName(string videoFolder, string levelName) {
        string videoName = levelName.Replace(" ", "");
        foreach (char c in Path.GetInvalidFileNameChars()) {
            videoName = videoName.Replace(c, '_');
        }
        return Path.Combine(videoFolder, $"Vidcutter_{videoName}{GetVideoSuffixForDuplicates(videoFolder, videoName)}.mp4");
    }

    public static bool IsPathAVideo(string videoPath) {
        return VideoFileExtensions.Contains(Path.GetExtension(videoPath).TrimStart('.'));
    }

    public static string GetLastVideoFile(string videoFolder) {
        return Directory.GetFiles(videoFolder)
            .Where(IsPathAVideo)
            .OrderByDescending(File.GetLastWriteTime)
            .FirstOrDefault();
    }
}
