using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

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

        string ffmpegBaseDir = Path.Combine(FileUtils.VidcutterWorkingDirectory, "ffmpeg");
        bool isInstalling = false;
        
        if (!Directory.Exists(ffmpegBaseDir)) {
            isInstalling = true;
            (installCallback ?? InstallFFmpeg)();
        }

        _ffmpegDirectory = Path.Combine(ffmpegBaseDir, "bin") + "/";
        _initialized = true;
        return isInstalling;
    }

    public static void InstallFFmpeg() { InstallFFmpeg(null); }
    
    public static void InstallFFmpeg(Func<int, long, int, bool> progressCallback) {
        const string downloadFolder = FileUtils.VidcutterWorkingDirectory;
        
        string fileName = OperatingSystem.IsWindows()
            ? "ffmpeg-master-latest-win64-gpl.zip"
            : "ffmpeg-master-latest-linux64-gpl.tar.xz";

        string downloadUrl = $"https://github.com/BtbN/FFmpeg-Builds/releases/latest/download/{fileName}";
        
        string downloadPath = Path.Combine(downloadFolder, fileName);
        string extractedPath = Path.Combine(downloadFolder, fileName.Split(".")[0]);
        try {
            Logger.Info("Vidcutter", $"Starting download of {downloadUrl}");
            if (!FileUtils.DownloadFFmpegFromUrl(downloadUrl, downloadPath, progressCallback))
                return;
            FileUtils.ExtractArchive(downloadPath, downloadFolder, cleanArchive: true);
            Directory.Move(extractedPath, Path.Combine(downloadFolder, "ffmpeg"));
        } catch (Exception ex) {
            Logger.Error("Vidcutter", ex.StackTrace+" "+ex.Message);
        }
    }

    private static bool IsFFmpegInPath() {
        try {
            CommandUtils.RunProcess("ffmpeg", "-version");
            return true;
        } catch (Win32Exception) {
            return false;
        }
    }
    
    private static Process FFmpeg(string arguments) {
        if (!_initialized) Initialize();
        return CommandUtils.CreateProcess($"{_ffmpegDirectory}ffmpeg", arguments);
    }
    
    private static Process FFprobe(string arguments) {
        if (!_initialized) Initialize();
        return CommandUtils.CreateProcess($"{_ffmpegDirectory}ffprobe", arguments);
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