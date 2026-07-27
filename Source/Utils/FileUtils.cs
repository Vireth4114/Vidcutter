using System;
using System.IO;
using System.IO.Compression;

namespace Celeste.Mod.Vidcutter.Utils;

public static class FileUtils
{
    public const string VidcutterWorkingDirectory = "./VidCutter";
    
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
    
    

    public static bool DownloadFFmpegFromUrl(
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
}