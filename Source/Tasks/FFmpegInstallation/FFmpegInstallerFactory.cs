using System;
using Celeste.Mod.Vidcutter.Progress;
using Celeste.Mod.Vidcutter.Services.FFmpeg;

namespace Celeste.Mod.Vidcutter.Tasks.FFmpegInstallation;

public static class FFmpegInstallerFactory {
    public static FFmpegInstallerBase Create(FFmpegContext ffmpegContext, IProgress progress) {
        if (OperatingSystem.IsWindows()) return new WindowsFFmpegInstallerBase(ffmpegContext, progress);
        if (OperatingSystem.IsLinux()) return new LinuxFFmpegInstallerBase(ffmpegContext, progress);
        
        throw new PlatformNotSupportedException("FFmpeg installation is not supported on this platform.");
    }
}