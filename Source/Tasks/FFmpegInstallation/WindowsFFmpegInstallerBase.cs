using System.IO.Compression;
using Celeste.Mod.Vidcutter.Progress;
using Celeste.Mod.Vidcutter.Services.FFmpeg;

namespace Celeste.Mod.Vidcutter.Tasks.FFmpegInstallation;

public class WindowsFFmpegInstallerBase(FFmpegContext ffmpegContext, IProgress progress) : FFmpegInstallerBase(ffmpegContext, progress) {
    protected override string DownloadFileName => "ffmpeg-master-latest-win64-gpl.zip";

    protected override void ExtractArchive(string downloadPath, string extractedPath) {
        ZipFile.ExtractToDirectory(downloadPath, extractedPath);
    }
}