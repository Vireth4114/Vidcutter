using System;
using Celeste.Mod.Vidcutter.Progress;

namespace Celeste.Mod.Vidcutter.Tasks.FFmpegInstallation;

public static class FFmpegInstallerFactory {
    public static FFmpegInstallerBase Create(IProgress progress) {
        if (OperatingSystem.IsWindows()) return new WindowsFFmpegInstallerBase(progress);
        if (OperatingSystem.IsLinux()) return new LinuxFFmpegInstallerBase(progress);
        
        throw new PlatformNotSupportedException("FFmpeg installation is not supported on this platform.");
    }
}