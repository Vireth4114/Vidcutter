using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Celeste.Mod.UI;
namespace Celeste.Mod.Vidcutter;

public class VideoCreation {
    public int crf;
    public string videoFolder;
    public string videoName;
    public OuiLoggedProgress progress;
    public TimeSpan delayStart;
    public TimeSpan delayEnd;

    public VideoCreation(OuiLoggedProgress progress = null, string videoName = null, int crf = 27) {
        this.progress = progress;
        this.crf = crf;
        this.videoName = videoName;

        string UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        videoFolder = Path.Combine(UserProfile, "Videos");

        delayStart = TimeSpan.FromSeconds(-0.8);
        delayEnd = TimeSpan.FromSeconds(1.3);
    }

    public List<string> GetAllVideos() {
        DateTime firstLog = VidcutterModule.getAllLogs()[0].Time;
        List<string> videos = new List<string>();
        foreach (string video in Directory.GetFiles(videoFolder)) {
            DateTime videoTime = File.GetCreationTime(video);
            if (videoTime >= firstLog) {
                videos.Add(video);
            }
        }
        return videos;
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

    public void ProcessVideoInit() {
        progress.Init<OuiModOptions>("Creating video...", new Task(ProcessVideo), 100);
    }

    public void ProcessVideo() {
        string video = Path.Combine(videoFolder, videoName);

        Process process = createProcess($"/C ffprobe -i \"{video}\" -show_entries format=duration -v quiet -of csv=\"p=0\"");
        process.Start();
        string strDuration = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        TimeSpan duration = TimeSpan.FromSeconds(double.Parse(strDuration));

        DateTime startVideo = File.GetCreationTime(video);
        DateTime endVideo = startVideo + duration;

        List<LoggedString> parsedLines = VidcutterModule.getAllLogs(startVideo, endVideo);
        List<LoggedString[]> processed = ProcessLogs(parsedLines);
        
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

    public List<LoggedString[]> ProcessLogs(List<LoggedString> parsedLines) {
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
        return processed;
    }
}