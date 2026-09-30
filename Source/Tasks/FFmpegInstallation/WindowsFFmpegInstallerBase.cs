using System.IO.Compression;
using Celeste.Mod.Vidcutter.Progress;

namespace Celeste.Mod.Vidcutter.Tasks.FFmpegInstallation;

public class WindowsFFmpegInstallerBase(IProgress progress) : FFmpegInstallerBase(progress) {
    protected override string DownloadFileName => "ffmpeg-master-latest-win64-gpl.zip";

    protected override void ExtractArchive(string downloadPath, string extractedPath) {
        ZipFile.ExtractToDirectory(downloadPath, extractedPath);
    }
}