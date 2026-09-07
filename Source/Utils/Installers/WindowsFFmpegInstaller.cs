using System.IO.Compression;

namespace Celeste.Mod.Vidcutter.Utils.Installers;

public class WindowsFFmpegInstaller: FFmpegInstaller {
    protected override string FileName => "ffmpeg-master-latest-win64-gpl.zip";

    protected override void ExtractArchive(string downloadPath, string extractedPath) {
        ZipFile.ExtractToDirectory(downloadPath, extractedPath);
    }
}