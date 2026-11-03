using Celeste.Mod.Vidcutter.Progress;
using Celeste.Mod.Vidcutter.Utils;

namespace Celeste.Mod.Vidcutter.Tasks.FFmpegInstallation;

public class LinuxFFmpegInstallerBase(IProgress progress) : FFmpegInstallerBase(progress) {
    protected override string DownloadFileName => "ffmpeg-master-latest-linux64-gpl.tar.xz";

    protected override void ExtractArchive(string downloadPath, string extractedPath) {
        CommandUtils.ExtractTarXz(downloadPath, extractedPath);
    }
}