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
    public List<ProcessedVideo> videos = new List<ProcessedVideo>();
    public OuiLoggedProgress progress;
    public VideoCreation(OuiLoggedProgress progress = null, int crf = 27) {
        this.progress = progress;
        this.crf = crf;
    }

    public List<string> GetAllVideos() {
        List<LoggedString> logs = VidcutterModule.getAllLogs();
        if (logs.Count == 0) {
            return new List<string>();
        }
        DateTime firstLog = logs[0].Time;
        List<string> videos = new List<string>();
        if (!Directory.Exists(VidcutterModule.Settings.VideoFolder)) {
            return videos;
        }
        string[] allVideos = Directory.GetFiles(VidcutterModule.Settings.VideoFolder);
        foreach (string video in allVideos) {
            DateTime videoTime = File.GetCreationTime(video);
            videoTime += TimeSpan.FromHours(5); // Hacky stuff to not use ffprobe but still giving leeway
            if (videoTime >= firstLog) {
                videos.Add(video);
            }
        }
        Logger.Info("Vidcutter", $"{videos.Count}/{allVideos.Count()} videos in {VidcutterModule.Settings.VideoFolder} are after start of log");
        return videos;
    }

    public static Process createProcess(string fileName, string arguments) {
        return new Process {
            StartInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                FileName = fileName,
                Arguments = arguments
            }
        };
    }

    public static TimeSpan? getVideoDuration(string video) {
        // This is slow, maybe segment display of videos to make it less visible ?
        Process process = createProcess($"{VidcutterModule.Settings.FFmpegPath}ffprobe",  $"-i \"{video}\" -show_entries format=duration -v quiet -of csv=\"p=0\"");
        process.Start();
        string strDuration = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (strDuration == "") {
            return null;
        }
        return TimeSpan.FromSeconds(double.Parse(strDuration));
    }

    public void ProcessVideosProgress() {
        progress.Init<OuiModOptions>(Dialog.Clean("VIDCUTTER_PROCESS_TITLE"), new Task(() => {
            int idx = 1;
            int videoIdx = 1;
            foreach (ProcessedVideo video in videos) {
                progress.LogLine(Dialog.Clean("VIDCUTTER_PROCESSINGVIDEO") + $" {video.Video} ({videoIdx++}/{videos.Count})");
                idx = ProcessVideo(video, idx);
            }
            ConcatAndClean(idx);
        }), 100);
    }

    public int ProcessVideo(ProcessedVideo processedVideo, int startIdx = 1) {
        Process process;
        string video = Path.Combine(VidcutterModule.Settings.VideoFolder, processedVideo.Video);
        DateTime startVideo = File.GetCreationTime(video);
        TimeSpan? duration = getVideoDuration(video);
        if (duration == null) {
            return startIdx;
        }
        DateTime endVideo = startVideo + (TimeSpan)duration;
        List<LoggedString[]> processed = ProcessLogs(startVideo, endVideo, processedVideo.Level);
        
        StreamWriter listVideos = new StreamWriter("./Vidcutter/videos.txt", true);
        int videoIdx = startIdx;
        foreach (LoggedString[] line in processed) {
            progress.Progress = 0;
            TimeSpan startTime;
            if (line[0] == null) {
                startTime = TimeSpan.Zero;
            } else {
                startTime =  line[0].Time + TimeSpan.FromSeconds(VidcutterModule.Settings.DelayStart) - startVideo;
            }
            TimeSpan endTime = line[1].Time + TimeSpan.FromSeconds(VidcutterModule.Settings.DelayEnd) - startVideo;
            double clipDuration = (endTime - startTime).TotalSeconds;
            string ss = $"{startTime:hh\\:mm\\:ss\\.fff}";
            string to = $"{endTime:hh\\:mm\\:ss\\.fff}";
            Logger.Info("Vidcutter", $"Processing clip from {ss} to {to}");
            process = createProcess($"{VidcutterModule.Settings.FFmpegPath}ffmpeg", $"-ss {ss} -to {to} -i \"{video}\" -c:a copy -map 0 -vcodec libx264 " +
                                    $"-crf {crf} -preset veryfast -y ./Vidcutter/{videoIdx}.mp4 -v warning -progress pipe:1");
            process.OutputDataReceived += (sender, e) => {
                if (e.Data?.StartsWith("out_time=") ?? false) {
                    string[] splitted = e.Data.Split('=');
                    if (TimeSpan.TryParse(splitted[1], out TimeSpan currentTime)) {
                        progress.Progress = (int) (currentTime.TotalSeconds / clipDuration * 100);
                    }
                }
            };
            progress.LogLine("- " + Dialog.Clean("VIDCUTTER_PROCESSINGCLIP") + $" {videoIdx - startIdx + 1}/{processed.Count}");
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            listVideos.WriteLine($"file '{videoIdx}.mp4'");
            videoIdx++;
        }
        listVideos.Close();
        return videoIdx;
    }

    public void ConcatAndClean(int videoCount) {
        Process process = createProcess($"{VidcutterModule.Settings.FFmpegPath}ffmpeg", 
                                        $"-f concat -safe 0 -i ./Vidcutter/videos.txt -c copy -map 0 -y" + 
                                        $"{VidcutterModule.Settings.VideoFolder}/Vidcutter_{videos[0].Level.Replace(" ", "")}.mp4");
        process.Start();
        process.WaitForExit();

        File.Delete("./Vidcutter/videos.txt");
        for (int i = 1; i < videoCount; i++) {
            File.Delete($"./Vidcutter/{i}.mp4");
        }
    }

    public static List<LoggedString[]> ProcessLogs(string video) {
        return ProcessLogs(VidcutterModule.getAllLogs(video));
    }

    public static List<LoggedString[]> ProcessLogs(DateTime startTime, DateTime endTime, string level = null) {
        return ProcessLogs(VidcutterModule.getAllLogs(startTime, endTime, level));
    }

    public static List<LoggedString[]> ProcessLogs(List<LoggedString> parsedLines) {
        List<LoggedString[]> processed = new List<LoggedString[]>();
        LoggedString lastDeath = null;
        for (int i = 0; i < parsedLines.Count; i++) {
            LoggedString previousLine;
            if (i == 0) {
                previousLine = null;
            } else {
                previousLine = parsedLines[i - 1];
            }
            LoggedString currentLine = parsedLines[i];
            LoggedString nextline;
            if (i == parsedLines.Count - 1) {
                nextline = null;
            } else {
                nextline = parsedLines[i + 1];
            }
            if (currentLine.Event == "RESTART CHAPTER") {
                processed.Clear();
                lastDeath = null;
                continue;
            }
            if (!new[] {"ROOM PASSED", "LEVEL COMPLETE"}.Contains(currentLine.Event))
                continue;
            if (previousLine != null && previousLine.Event == "STATE")
                continue;
            
            if (nextline == null || nextline.Event == "DEATH") {
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
