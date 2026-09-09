using System.IO;

namespace Celeste.Mod.Vidcutter.Utils;

public static class FileUtils {
    public static readonly string VidcutterWorkingDirectory = Path.Combine(".", "VidCutter");
    public static readonly string DurationCacheFile = Path.Combine(VidcutterWorkingDirectory, "durationCache.txt");
    public static readonly string ClipsIndexFile = Path.Combine(VidcutterWorkingDirectory, "videos.txt");
}