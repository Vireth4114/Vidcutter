using System;
using System.IO;
using static Celeste.Mod.Vidcutter.Utils.FileUtils;

namespace Celeste.Mod.Vidcutter.Utils.Installers;

public abstract class FFmpegInstaller {
    private const string BaseDownloadUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/latest/download";
    protected abstract string FileName { get; }
    
    public void InstallFFmpeg(Func<int, long, int, bool> progressCallback) {
        string downloadFolder = VidcutterWorkingDirectory;
        
        string downloadUrl = $"{BaseDownloadUrl}/{FileName}";
        
        string downloadPath = Path.Combine(downloadFolder, FileName);
        string extractedPath = Path.Combine(downloadFolder, FileName.Split(".")[0]);
        try {
            Logger.Info("Vidcutter", $"Starting download of {downloadUrl}");
            if (!DownloadFFmpegFromUrl(downloadUrl, downloadPath, progressCallback))
                return;
            
            ExtractArchive(downloadPath, downloadFolder);
            if (File.Exists(downloadPath))
                File.Delete(downloadPath);
            
            Directory.Move(extractedPath, Path.Combine(downloadFolder, "ffmpeg"));
        } catch (Exception ex) {
            Logger.Error("Vidcutter", ex.StackTrace+" "+ex.Message);
        }
    }

    private static bool DownloadFFmpegFromUrl(
        string downloadUrl,
        string downloadFilePath, 
        Func<int, long, int, bool> progressCallback = null
    ) {
        Everest.Updater.DownloadFileWithProgress(downloadUrl, downloadFilePath, progressCallback ?? ((_, _, _) => true));

        if (File.Exists(downloadFilePath))
            return true;
        
        Logger.Error("Vidcutter", $"Download failed! The file went missing");
        return false;
    }

    protected abstract void ExtractArchive(string downloadPath, string extractedPath);
}