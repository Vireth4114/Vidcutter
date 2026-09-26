using System.Diagnostics;
using System.IO;

namespace Celeste.Mod.Vidcutter.Utils;

public static class CommandUtils {
    private static Process CreateProcess(string fileName, string arguments, bool redirectOutput = false, bool redirectError = false) {
        Logger.Info("Vidcutter", $"Executing {fileName} {arguments}");
        return new Process {
            StartInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = redirectError,
                FileName = fileName,
                Arguments = arguments
            }
        };
    }

    private static Process Run(Process process) {
        process.Start();
        process.WaitForExit();
        return process;
    }

    private static Process FFmpeg(string arguments, string ffmpegDirectory, bool redirectOutput = false) {
        return CreateProcess(Path.Combine(ffmpegDirectory, "ffmpeg"), arguments, redirectOutput: redirectOutput);
    }
    
    private static Process FFprobe(string arguments, string ffmpegDirectory) {
        return CreateProcess(Path.Combine(ffmpegDirectory, "ffprobe"), arguments, redirectOutput: true, redirectError: true);
    }

    public static bool IsFFmpegInPath() {
        return false;
        // try {
        //     Run(FFmpeg("version", ""));
        //     return true;
        // } catch (Win32Exception) {
        //     return false;
        // }
    }

    public static Process GetDurationStringWithFFprobe(string filePath, string ffmpegDirectory) {
        return Run(FFprobe($"-i \"{filePath}\" -show_entries format=duration -v error -of csv=\"p=0\"", ffmpegDirectory));
    }

    public static Process GetCutClipProcess(string from, string to, string videoPath, string output, string ffmpegDirectory, int crf = 27) {
        return FFmpeg(
            $"-ss {from} -to {to} -i \"{videoPath}\" -c:a copy -map 0 -vcodec libx264 " +
            $"-crf {crf} -preset veryfast -y \"{output}\" -v warning -progress pipe:1",
            ffmpegDirectory,
            redirectOutput: true
        );
    }

    public static void ConcatenateClipFromFile(string indexFilePath, string output, string ffmpegDirectory) {
        Run(FFmpeg($"-f concat -safe 0 -i \"{indexFilePath}\" -c:v copy -map 0 -y \"{output}\"", ffmpegDirectory));
    }

    public static void ExtractTarXz(string archivePath, string outputPath) {
        Run(CreateProcess("tar", $"-xf \"{archivePath}\" -C {outputPath}"));
    }
}