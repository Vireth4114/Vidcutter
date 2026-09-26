using System.IO.Compression;
using Celeste.Mod.Vidcutter.Progress;

namespace Celeste.Mod.Vidcutter.Tasks.FFmpegInstallation;

public class WindowsFFmpegInstaller(IProgress progress) : FFmpegInstaller(progress) {
    protected override string DownloadFileName => "ffmpeg-master-latest-win64-gpl.zip";

    protected override void ExtractArchive(string downloadPath, string extractedPath) {
        ZipFile.ExtractToDirectory(downloadPath, extractedPath);
    }
}