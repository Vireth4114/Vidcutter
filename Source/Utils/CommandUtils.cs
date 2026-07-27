using System.Diagnostics;

namespace Celeste.Mod.Vidcutter.Utils;

public static class CommandUtils {
    public static Process CreateProcess(string fileName, string arguments) {
        Logger.Info("Vidcutter", $"Executing {fileName} {arguments}");
        return new Process {
            StartInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = fileName,
                Arguments = arguments
            }
        };
    }

    public static void RunProcess(string fileName, string arguments) {
        Process process = CreateProcess(fileName, arguments);
        process.Start();
        process.WaitForExit();
    }
}