using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Celeste.Mod.Vidcutter.Models;
using Celeste.Mod.Vidcutter.Utils.Installers;
using static Celeste.Mod.Vidcutter.Utils.FileUtils;

namespace Celeste.Mod.Vidcutter.Utils;

public static class FFmpegUtils {
    private static string _ffmpegDirectory;
    private static bool _initialized;

    /**
     * Initialize FFmpeg, installing it locally if it's not in the path
     *
     * @return true if installation has to be done
     */
    public static bool Initialize(Action installCallback = null) {
        if (_initialized) return false;
        
        if (IsFFmpegInPath()) {
            _ffmpegDirectory = "";
            _initialized = true;
            return false;
        }

        RemoveLegacyFFmpegIfItExists();

        string ffmpegBaseDir = Path.Combine(VidcutterWorkingDirectory, "ffmpeg");
        bool isInstalling = false;
        
        if (!Directory.Exists(ffmpegBaseDir)) {
            isInstalling = true;
            if (installCallback != null) {
                installCallback();
            } else {
                InstallFFmpeg();
            }
        }

        _ffmpegDirectory = Path.Combine(ffmpegBaseDir, "bin") + Path.DirectorySeparatorChar;
        _initialized = true;
        return isInstalling;
    }

    private static void RemoveLegacyFFmpegIfItExists() {
        string ffmpegBaseDir = Path.Combine(VidcutterWorkingDirectory, "ffmpeg");
        string ffmpegBinDir = Path.Combine(ffmpegBaseDir, "bin");
        if (Directory.Exists(ffmpegBaseDir) && !Directory.Exists(ffmpegBinDir)) {
            Logger.Warn("Vidcutter",
                $"FFmpeg directory {ffmpegBaseDir} exists but does not contain a bin directory. Reinstalling FFmpeg.");
            Directory.Delete(ffmpegBaseDir, recursive: true);
        }
    }

    public static void InstallFFmpeg(Func<int, long, int, bool> progressCallback = null) {
        FFmpegInstaller installer;
        if (OperatingSystem.IsWindows()) {
            installer = new WindowsFFmpegInstaller();
        } else if (OperatingSystem.IsLinux()) {
            installer = new LinuxFFmpegInstaller();
        } else {
            throw new PlatformNotSupportedException("FFmpeg installation is not supported on this platform.");
        }
        installer.InstallFFmpeg(progressCallback);
    }

    private static bool IsFFmpegInPath() {
        try {
            CommandUtils.RunProcess("ffmpeg", "-version");
            return true;
        } catch (Win32Exception) {
            return false;
        }
    }
    
    private static Process FFmpeg(string arguments, bool redirectOutput = false) {
        if (!_initialized) Initialize();
        return CommandUtils.CreateProcess($"{_ffmpegDirectory}ffmpeg", arguments, redirectOutput: redirectOutput);
    }
    
    private static Process FFprobe(string arguments) {
        if (!_initialized) Initialize();
        return CommandUtils.CreateProcess($"{_ffmpegDirectory}ffprobe", arguments, redirectOutput: true, redirectError: true);
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
            $"-crf {VidcutterModule.Settings.CRF} -preset veryfast -y \"{output}\" -v warning -progress pipe:1",
            redirectOutput: onProgress != null
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