using System;

namespace Celeste.Mod.Vidcutter.Utils.Installers;

public static class FFmpegInstallerFactory {
    public static FFmpegInstaller Create(IProgress progress) {
        if (OperatingSystem.IsWindows()) return new WindowsFFmpegInstaller(progress);
        if (OperatingSystem.IsLinux()) return new LinuxFFmpegInstaller(progress);
        
        throw new PlatformNotSupportedException("FFmpeg installation is not supported on this platform.");
    }
}