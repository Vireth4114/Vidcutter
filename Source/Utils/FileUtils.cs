using System;
using System.IO;
using System.IO.Compression;

namespace Celeste.Mod.Vidcutter.Utils;

public static class FileUtils {
    public static void ExtractZip(string zipFilePath, string destinationDirectory) {
        ZipFile.ExtractToDirectory(zipFilePath, destinationDirectory);
    }

    public static void ExtractTarXz(string tarFilePath, string destinationDirectory) {
        CommandUtils.RunProcess("tar", $"-xf \"{tarFilePath}\" -C {destinationDirectory}");
    }

    public static void ExtractArchive(string filePath, string destinationDirectory, bool cleanArchive = false) {
        if (filePath.EndsWith(".zip")) {
            ExtractZip(filePath, destinationDirectory);
        } else if (filePath.EndsWith(".tar.xz") || filePath.EndsWith(".txz")) {
            ExtractTarXz(filePath, destinationDirectory);
        } else {
            throw new NotSupportedException($"Unsupported archive format: {filePath}");
        }
        
        if (cleanArchive && File.Exists(filePath))
            File.Delete(filePath);
    }
    
    

    public static bool DownloadFFmpegFromUrl(string downloadUrl, string downloadFilePath, OuiVidcutterProgress progress = null) {
        progress?.LogLine(Dialog.Clean("VIDCUTTER_DOWNLOADINGFFMPEG"));
        Everest.Updater.DownloadFileWithProgress(downloadUrl, downloadFilePath, (position, length, speed) => {
            if (progress == null)
                return true;
                    
            if (length > 0) {
                progress.Lines[^1] =
                    Dialog.Clean("VIDCUTTER_DOWNLOADINGFFMPEG") + $" {(int) Math.Floor(100D * (position / (double) length))}% @ {speed} KiB/s";
                progress.Progress = position;
            } else {
                progress.Lines[^1] =
                    Dialog.Clean("VIDCUTTER_DOWNLOADINGFFMPEG") + $" {(int) Math.Floor(position / 1000D)}KiB @ {speed} KiB/s";
            }

            progress.ProgressMax = (int) length;
            return true;
        });

        if (File.Exists(downloadFilePath))
            return true;
        
        Logger.Error("Vidcutter", $"Download failed! The file went missing");
        return false;
    }
}