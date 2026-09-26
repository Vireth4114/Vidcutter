using System;
using System.IO;
using System.Threading.Tasks;
using Celeste.Mod.Vidcutter.Progress;
using Celeste.Mod.Vidcutter.Utils;
using static Celeste.Mod.Vidcutter.Utils.FileConstants;

namespace Celeste.Mod.Vidcutter.Tasks.FFmpegInstallation;

public abstract class FFmpegInstaller(IProgress progress) {
    private const string BaseDownloadUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/latest/download";
    
    private static string FFmpegBaseDirectory => Path.Combine(VidcutterWorkingDirectory, "ffmpeg");
    private static string FFmpegBinDirectory => Path.Combine(FFmpegBaseDirectory, "bin");
    
    protected abstract string DownloadFileName { get; }
    public string FFmpegDirectory { get; private set; }

    private bool _isInstalling;

    public bool IsFFmpegInstalled() {
        if (CommandUtils.IsFFmpegInPath()) {
            FFmpegDirectory = "";
            return true;
        }

        if (Directory.Exists(FFmpegBinDirectory)) {
            FFmpegDirectory = FFmpegBinDirectory;
            return true;
        }

        return false;
    }

    public void InstallFFmpegAsynchronously(Action onComplete = null) {
        if (_isInstalling)
            return;
        _isInstalling = true;
        progress.Task = new Task(InstallFFmpegTask);
        progress.OnComplete += () => {
            _isInstalling = false;
            FFmpegDirectory = FFmpegBinDirectory;
            onComplete?.Invoke();
        };
        progress.Start();
    }

    private void InstallFFmpegTask() {
        RemoveLegacyFFmpegIfItExists();
        string downloadUrl = $"{BaseDownloadUrl}/{DownloadFileName}";

        string downloadPath = Path.Combine(VidcutterWorkingDirectory, DownloadFileName);
        string extractedPath = Path.Combine(VidcutterWorkingDirectory, DownloadFileName.Split(".")[0]);

        try {
            Logger.Info("Vidcutter", $"Starting download of {downloadUrl}");
            if (!DownloadFFmpegFromUrl(downloadUrl, downloadPath))
                return;

            progress.Message = Dialog.Clean("VIDCUTTER_EXTRACTINGFFMPEG");
            ExtractArchive(downloadPath, VidcutterWorkingDirectory);

            if (File.Exists(downloadPath))
                File.Delete(downloadPath);

            Directory.Move(extractedPath, FFmpegBaseDirectory);
        } catch (Exception ex) {
            Logger.Error("Vidcutter", ex.StackTrace + " " + ex.Message);
        }
    }

    private bool DownloadFFmpegFromUrl(string downloadUrl, string downloadFilePath) {
        Everest.Updater.DownloadFileWithProgress(downloadUrl, downloadFilePath, HandleProgressFromEverestUpdater);

        if (File.Exists(downloadFilePath))
            return true;

        Logger.Error("Vidcutter", $"Download failed! The file went missing");
        return false;
    }

    private bool HandleProgressFromEverestUpdater(int position, long length, int speed) {
        string messageProgress;
        if (length > 0) {
            int currentProgress = (int) Math.Floor(100D * (position / (double) length));
            progress.Progress = currentProgress / 100f;
            messageProgress = $"{currentProgress}%";
        } else {
            messageProgress = $"{(int) Math.Floor(position / 1000D)}KiB";
        }
        progress.Message = Dialog.Clean("VIDCUTTER_DOWNLOADINGFFMPEG") + $" {messageProgress} @ {speed} KiB/s";
        return true;
    }

    private static void RemoveLegacyFFmpegIfItExists() {
        if (Directory.Exists(FFmpegBaseDirectory) && !Directory.Exists(FFmpegBinDirectory)) {
            Directory.Delete(FFmpegBaseDirectory, recursive: true);
        }
    }

    protected abstract void ExtractArchive(string downloadPath, string extractedPath);
}
