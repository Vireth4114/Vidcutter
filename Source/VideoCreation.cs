using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Celeste.Mod.UI;
namespace Celeste.Mod.Vidcutter;

public class VideoCreation {

    public static List<LoggedString> getAllLogs(DateTime? startVideo = null, DateTime? endVideo = null) {
        VidcutterModule.LogFileWriter.Close();
        string[] lines = File.ReadAllLines(VidcutterModule.logPath);
        List<LoggedString> parsedLines = new List<LoggedString>();
        foreach (string line in lines) {
            DateTime logTime = DateTime.Parse(line.Substring(1, 23));
            bool condition = true;
            if (startVideo != null) {
                condition &= startVideo <= logTime;
            }
            if (endVideo != null) {
                condition &= logTime <= endVideo;
            }
            if (condition) {
                string[] loggedEvent = line.Substring(26).Split(" | ");
                parsedLines.Add(new LoggedString(logTime, loggedEvent[2], loggedEvent[0], loggedEvent[1]));
            }
        }

        VidcutterModule.LogFileWriter = new StreamWriter(VidcutterModule.logPath, true) {
            AutoFlush = true
        };
        return parsedLines;
    }

    public static List<string> GetAllVideos() {
        string UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string videoFolder = Path.Combine(UserProfile, "Videos");
        DateTime firstLog = getAllLogs()[0].Time;
        List<string> videos = new List<string>();
        foreach (string video in Directory.GetFiles(videoFolder)) {
            DateTime videoTime = File.GetCreationTime(video);
            if (videoTime >= firstLog) {
                videos.Add(video);
            }
        }
        return videos;
    }

    public static void ProcessVideoInit(OuiLoggedProgress progress, int crf) {
        progress.Init<OuiModOptions>("Creating video...", new Task(() => {
            ProcessVideo(progress, crf);
        }), 100);
    }

    public static Process createProcess(string arguments) {
        return new Process {
            StartInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                FileName = "cmd.exe",
                Arguments = arguments
            }
        };
    }

    public static void ProcessVideo(OuiLoggedProgress progress, int crf) {
        string videoFolder = "C:/Users/rapha/Videos";
        TimeSpan delayStart = TimeSpan.FromSeconds(-0.8);
        TimeSpan delayEnd = TimeSpan.FromSeconds(1.3);

        string[] files = Directory.GetFiles(videoFolder, "20*.mp4");
        string video = files[files.Length - 1];

        Process process = createProcess($"/C ffprobe -i \"{video}\" -show_entries format=duration -v quiet -of csv=\"p=0\"");
        process.Start();
        string strDuration = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        TimeSpan duration = TimeSpan.FromSeconds(double.Parse(strDuration));

        DateTime startVideo = File.GetCreationTime(video);
        DateTime endVideo = startVideo + duration;

        List<LoggedString> parsedLines = getAllLogs(startVideo, endVideo);

        List<LoggedString[]> processed = new List<LoggedString[]>();
        LoggedString lastDeath = null;
        for (int i = 1; i < parsedLines.Count - 1; i++) {
            LoggedString previousLine = parsedLines[i - 1];
            LoggedString currentLine = parsedLines[i];
            LoggedString nextline = parsedLines[i + 1];
            if (!new[] {"ROOM PASSED", "LEVEL COMPLETE"}.Contains(currentLine.Event))
                continue;
            if (previousLine.Event == "STATE")
                continue;
            
            if (currentLine.Event == "LEVEL COMPLETE" || nextline.Event == "DEATH") {
                if (lastDeath == null) {
                    processed.Add([previousLine, currentLine]);
                } else {
                    processed.Add([lastDeath, currentLine]);
                    lastDeath = null;
                }
            } else if (lastDeath == null) {
                lastDeath = previousLine;
            }
        }
        StreamWriter listVideos = new StreamWriter("./Vidcutter/videos.txt");
        int videoIdx = 1;
        foreach (LoggedString[] line in processed) {
            progress.Progress = 0;
            TimeSpan startTime = line[0].Time + delayStart - startVideo;
            TimeSpan endTime = line[1].Time + delayEnd - startVideo;
            double clipDuration = (endTime - startTime).TotalSeconds;
            string ss = $"{startTime:hh\\:mm\\:ss\\.fff}";
            string to = $"{endTime:hh\\:mm\\:ss\\.fff}";
            process = createProcess($"/C ffmpeg -ss {ss} -to {to} -i \"{video}\" -vcodec libx264 " +
                                    $"-crf {crf} -preset veryfast -y ./Vidcutter/{videoIdx}.mp4 -v warning -progress pipe:1");

            process.OutputDataReceived += (sender, e) => {
                if (e.Data?.StartsWith("out_time=") ?? false) {
                    string[] splitted = e.Data.Split('=');
                    if (TimeSpan.TryParse(splitted[1], out TimeSpan currentTime)) {
                        progress.Progress = (int) (currentTime.TotalSeconds / clipDuration * 100);
                    }
                }
            };
            progress.LogLine($"Processing clip {videoIdx}/{processed.Count}");
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            listVideos.WriteLine($"file '{videoIdx}.mp4'");
            videoIdx++;
        }
        listVideos.Close();

        process = createProcess($"/C ffmpeg -f concat -safe 0 -i ./Vidcutter/videos.txt -c copy -y ./Vidcutter/output.mp4");
        process.Start();
        process.WaitForExit();

        File.Delete("./Vidcutter/videos.txt");
        for (int i = 1; i < videoIdx; i++) {
            File.Delete($"./Vidcutter/{i}.mp4");
        }
    }
}