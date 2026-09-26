using System.IO.Compression;

namespace Celeste.Mod.Vidcutter.Utils.Installers;

public class WindowsFFmpegInstaller(IProgress progress) : FFmpegInstaller(progress) {
    protected override string DownloadFileName => "ffmpeg-master-latest-win64-gpl.zip";

    protected override void ExtractArchive(string downloadPath, string extractedPath) {
        ZipFile.ExtractToDirectory(downloadPath, extractedPath);
    }
}