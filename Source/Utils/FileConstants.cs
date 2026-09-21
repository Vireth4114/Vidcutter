using System.IO;

namespace Celeste.Mod.Vidcutter.Utils;

public static class FileConstants {
    public static readonly string VidcutterWorkingDirectory = Path.Combine(".", "VidCutter");

    public static string LogFile {
        get {
            string logFolder = Path.Combine(VidcutterWorkingDirectory, Path.Combine("logs"));
            if (!Directory.Exists(logFolder)) {
                Directory.CreateDirectory(logFolder);
            }
            return Path.Combine(logFolder, "log.txt");
        }
    }
}