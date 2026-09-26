namespace Celeste.Mod.Vidcutter.Utils.Installers;

public class LinuxFFmpegInstaller(IProgress progress) : FFmpegInstaller(progress) {
    protected override string DownloadFileName => "ffmpeg-master-latest-linux64-gpl.tar.xz";

    protected override void ExtractArchive(string downloadPath, string extractedPath) {
        CommandUtils.ExtractTarXz(downloadPath, extractedPath);
    }
}