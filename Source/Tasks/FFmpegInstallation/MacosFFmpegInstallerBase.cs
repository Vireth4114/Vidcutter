using System.IO.Compression;
using Celeste.Mod.Vidcutter.Progress;
using Celeste.Mod.Vidcutter.Services.FFmpeg;

namespace Celeste.Mod.Vidcutter.Tasks.FFmpegInstallation;

public class MacosFFmpegInstallerBase(FFmpegContext ffmpegContext, IProgress progress) : FFmpegInstallerBase(ffmpegContext, progress) {
    protected override string DownloadUrl => "https://library.risingsunlight.dev/celeste/vidcutter/ffmpeg-9_0_2-macos.zip";

    protected override void ExtractArchive(string downloadPath, string extractedPath) {
        ZipFile.ExtractToDirectory(downloadPath, extractedPath);
    }
}