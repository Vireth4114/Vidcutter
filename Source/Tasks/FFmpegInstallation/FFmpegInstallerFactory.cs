using System;
using Celeste.Mod.Vidcutter.Progress;

namespace Celeste.Mod.Vidcutter.Tasks.FFmpegInstallation;

public static class FFmpegInstallerFactory {
    public static FFmpegInstaller Create(IProgress progress) {
        if (OperatingSystem.IsWindows()) return new WindowsFFmpegInstaller(progress);
        if (OperatingSystem.IsLinux()) return new LinuxFFmpegInstaller(progress);
        
        throw new PlatformNotSupportedException("FFmpeg installation is not supported on this platform.");
    }
}