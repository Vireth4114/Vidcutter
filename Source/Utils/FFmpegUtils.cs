using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace Celeste.Mod.Vidcutter.Utils;

public static class FFmpegUtils {
    private static string _ffmpegDirectory;
    private static bool _initialized;

    public static void Initialize() {
        if (!_initialized) return;
        
        if (IsFFmpegInPath()) {
            _ffmpegDirectory = "";
            _initialized = true;
            return;
        }
        
        if (!Directory.Exists(Path.Combine("./VidCutter/", "ffmpeg", "ffmpeg"))) {
            VidcutterModule.InstallFFmpeg();
        }
        
        _ffmpegDirectory = Path.Combine("./VidCutter/", "ffmpeg", "ffmpeg", "ffmpeg-master-latest-linux64-gpl", "bin") + "/";
        _initialized = true;
    }

    private static bool IsFFmpegInPath() {
        try {
            Process process = CreateProcess("ffmpeg", "-version");
            process.Start();
            process.WaitForExit();
            return true;
        } catch (Win32Exception) {
            return false;
        }
    }
    
    private static Process CreateProcess(string fileName, string arguments) {
        Logger.Info("Vidcutter", $"Executing {fileName} {arguments}");
        return new Process {
            StartInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = fileName,
                Arguments = arguments
            }
        };
    }
    
    private static Process FFmpeg(string arguments) {
        if (_initialized) Initialize();
        return CreateProcess($"{_ffmpegDirectory}ffmpeg", arguments);
    }
    
    private static Process FFprobe(string arguments) {
        if (_initialized) Initialize();
        return CreateProcess($"{_ffmpegDirectory}ffprobe", arguments);
    }

    public static void ConcatenateClipsFromIndexFilePath(string indexFilePath, string output) {
        Process process = FFmpeg($"-f concat -safe 0 -i \"{indexFilePath}\" -c:v copy -map 0 -y \"{output}\"");
        process.Start();
        process.WaitForExit();
    }

    public static Process NonBlockingCutClip(VideoFile video, TimeSpan startClip, TimeSpan endClip, string output, Action<TimeSpan> onProgress = null) {
        string ss = $@"{startClip:hh\:mm\:ss\.fff}";
        string to = $@"{endClip:hh\:mm\:ss\.fff}";
        
        Process process = FFmpeg(
            $"-ss {ss} -to {to} -i \"{video.FilePath}\" -c:a copy -map 0 -vcodec libx264 " +
            $"-crf {VidcutterModule.Settings.CRF} -preset veryfast -y \"{output}\" -v warning -progress pipe:1"
        );
        
        if (onProgress != null) {
            process.OutputDataReceived += (_, e) => {
                if (e.Data?.StartsWith("out_time=") ?? false) {
                    string stringContainingTimeProcessed = e.Data.Split('=')[1];
                    if (TimeSpan.TryParse(stringContainingTimeProcessed, out TimeSpan timeProcessed)) {
                        onProgress(timeProcessed);
                    }
                }
            };
        }
        
        process.EnableRaisingEvents = true;
        process.Start();
        process.BeginOutputReadLine();
        return process;
    }

    public static void CutClip(VideoFile video, TimeSpan startClip, TimeSpan endClip, string output, Action<TimeSpan> onProgress = null) {
        Process process = NonBlockingCutClip(video, startClip, endClip, output, onProgress);
        process.WaitForExit();
    }

    public static string GetDurationString(string filePath) {
        Process process = FFprobe($"-i \"{filePath}\" -show_entries format=duration -v quiet -of csv=\"p=0\"");
        process.Start();
        string strDuration = process.StandardOutput.ReadToEnd();
        string strError =  process.StandardError.ReadToEnd();
        if (strError.Length > 0) {
            throw new InvalidOperationException($"ffprobe returned an error for file {filePath}: {strError}");
        }
        process.WaitForExit();
        return strDuration;
    }
}