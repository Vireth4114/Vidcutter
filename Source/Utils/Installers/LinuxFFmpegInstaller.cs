namespace Celeste.Mod.Vidcutter.Utils.Installers;

public class LinuxFFmpegInstaller: FFmpegInstaller {
    protected override string FileName => "ffmpeg-master-latest-linux64-gpl.tar.xz";

    protected override void ExtractArchive(string downloadPath, string extractedPath) {
        CommandUtils.RunProcess("tar", $"-xf \"{downloadPath}\" -C {extractedPath}");
    }
}