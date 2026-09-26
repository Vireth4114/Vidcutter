using System;
using System.Diagnostics;
using Celeste.Mod.Vidcutter.Utils;

namespace Celeste.Mod.Vidcutter.Services;

public class FFmpegService(string ffmpegDirectory, int crf) {
    public void ConcatenateClipsFromIndexFilePath(string indexFilePath, string output) {
        CommandUtils.ConcatenateClipFromFile(indexFilePath, output, ffmpegDirectory);
    }
    
    public void CutClip(string videoPath, TimeSpan startClip, TimeSpan endClip, string output, Action<float> updateProgress = null) {
        string ss = $@"{startClip:hh\:mm\:ss\.fff}";
        string to = $@"{endClip:hh\:mm\:ss\.fff}";

        Process process = CommandUtils.GetCutClipProcess(ss, to, videoPath, output, ffmpegDirectory, crf);

        if (updateProgress != null) {
            process.OutputDataReceived += (_, e) => {
                if (e.Data?.StartsWith("out_time=") ?? false) {
                    string stringContainingTimeProcessed = e.Data.Split('=')[1];
                    if (TimeSpan.TryParse(stringContainingTimeProcessed, out TimeSpan timeProcessed)) {
                        updateProgress((float) (timeProcessed.TotalSeconds / (endClip - startClip).TotalSeconds));
                    }
                }
            };
        }

        process.EnableRaisingEvents = true;
        process.Start();
        process.BeginOutputReadLine();
        process.WaitForExit();
    }

    public string GetDurationFromFFprobe(string filePath) {
        Process process = CommandUtils.GetDurationStringWithFFprobe(filePath, ffmpegDirectory);
        string strDuration = process.StandardOutput.ReadToEnd();
        string strError = process.StandardError.ReadToEnd();
        if (strError.Length > 0) {
            throw new InvalidOperationException($"ffprobe returned an error for file {filePath}: {strError}");
        }
        return strDuration;
    }
}