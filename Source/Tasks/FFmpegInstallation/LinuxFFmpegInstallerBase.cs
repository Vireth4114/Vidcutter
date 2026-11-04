using Celeste.Mod.Vidcutter.Progress;
using Celeste.Mod.Vidcutter.Services.FFmpeg;
using Celeste.Mod.Vidcutter.Utils;

namespace Celeste.Mod.Vidcutter.Tasks.FFmpegInstallation;

public class LinuxFFmpegInstallerBase(FFmpegContext ffmpegContext, IProgress progress) : FFmpegInstallerBase(ffmpegContext, progress) {
    protected override string DownloadUrl => "https://github.com/BtbN/FFmpeg-Builds/releases/latest/download/ffmpeg-master-latest-linux64-gpl.tar.xz";

    protected override void ExtractArchive(string downloadPath, string extractedPath) {
        CommandUtils.ExtractTarXz(downloadPath, extractedPath);
    }
}