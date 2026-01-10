using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Celeste.Mod.UI;
namespace Celeste.Mod.Vidcutter;

public class VideoCreation {
    public int crf;
    public List<ProcessedVideo> videos = new List<ProcessedVideo>();
    public OuiVidcutterProgress progress;
    public VideoCreation(OuiVidcutterProgress progress = null, int crf = 27) {
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
        Logger.Info("Vidcutter", $"Executing {fileName} {arguments}");
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
        return getVideoDuration(video, out _);
    }

    public static TimeSpan? getVideoDuration(string video, out bool isFinished) {
        isFinished = true;
        if (VidcutterModule.DurationCache.ContainsKey(video)) {
            return VidcutterModule.DurationCache[video];
        }
        Process process = createProcess($"{VidcutterModule.Settings.FFmpegPath}ffprobe",  $"-i \"{video}\" -show_entries format=duration -v quiet -of csv=\"p=0\"");
        process.Start();
        string strDuration = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (!double.TryParse(strDuration, out double durationDouble) || durationDouble <= 0) {
            isFinished = false;
            return new[] { ".mkv", ".flv", ".ts" }.Contains(Path.GetExtension(video).ToLower())
                ? File.GetLastWriteTime(video) - File.GetCreationTime(video) + TimeSpan.FromSeconds(3) // Give time to make sure end of video is good
                : null;
        }
        TimeSpan duration = TimeSpan.FromSeconds(durationDouble);
        VidcutterModule.writeCache(video, duration);
        return duration;
    }

    public void ProcessVideosProgress(bool withDelete = false) {
        progress.Init<OuiModOptions>(Dialog.Clean("VIDCUTTER_PROCESS_TITLE"), new Task(() => {
            int idx = 1;
            int videoIdx = 1;
            foreach (ProcessedVideo video in videos) {
                progress.LogLine(Dialog.Clean("VIDCUTTER_PROCESSINGVIDEO") + $" {video.Video} ({videoIdx++}/{videos.Count})");
                idx = ProcessVideo(video, idx);
            }
            ConcatAndClean(idx);
            if (withDelete) {
                VidcutterModule.deleteLogs(videos);
            }
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
            float delay = VidcutterModule.Settings.DelayEnd;
            if (line[1].Event == "LEVEL COMPLETE") {
                delay = VidcutterModule.Settings.DelayComplete;
            }
            TimeSpan endTime = line[1].Time + TimeSpan.FromSeconds(delay) - startVideo;
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
        string output = getOutputVideoName(videos[0].Level);
        Logger.Info("Vidcutter", $"Concatenating {videoCount} videos into {output}");
        Process process = createProcess($"{VidcutterModule.Settings.FFmpegPath}ffmpeg", 
                                        $"-f concat -safe 0 -i ./Vidcutter/videos.txt -c:v copy -map 0 -y " +
                                        $"\"{output}\"");
        process.Start();
        process.WaitForExit();

        File.Delete("./Vidcutter/videos.txt");
        for (int i = 1; i < videoCount; i++) {
            File.Delete($"./Vidcutter/{i}.mp4");
        }
    }

    public static string getOutputVideoName(string levelName) {
        string videoName = levelName.Replace(" ", "");
        int outputNumber = 0;
        foreach (string file in Directory.GetFiles(VidcutterModule.Settings.VideoFolder)) {
            Regex regex = new Regex(@$".*\\Vidcutter_{Regex.Escape(videoName)}_?(\d+)?\.mp4");
            Match match = regex.Match(file);
            Logger.Info("Vidcutter", $"Checking existing file {file} against pattern {regex}: Match success: {match.Success}");
            if (match.Success) {
                if (match.Groups.Count > 1 && match.Groups[1].Success) {
                    outputNumber = Math.Max(int.Parse(match.Groups[1].Value), outputNumber);
                } else {
                    outputNumber = Math.Max(1, outputNumber);
                }
            }
        }
        foreach (char c in Path.GetInvalidFileNameChars()) {
            videoName = videoName.Replace(c, '_');
        }
        string output = $"{VidcutterModule.Settings.VideoFolder}\\Vidcutter_{videoName}";
        if (outputNumber > 0) {
            output += $"_{outputNumber + 1}";
        }
        output += ".mp4";
        return output;
    }

    public static List<LoggedString[]> ProcessLogs(string video) {
        return ProcessLogs(VidcutterModule.getAllLogs(video));
    }

    public static List<LoggedString[]> ProcessLogs(DateTime startTime, DateTime endTime, string level = null) {
        return ProcessLogs(VidcutterModule.getAllLogs(startTime, endTime, level));
    }

    public static List<LoggedString[]> ProcessLogs(List<LoggedString> parsedLines) {
        List<LoggedString[]> processed = new List<LoggedString[]>();
        List<LoggedString> currentClip = new List<LoggedString>();
        for (int i = 0; i < parsedLines.Count; i++) {
            LoggedString previousLine = i > 0 ? parsedLines[i - 1] : null;
            LoggedString currentLine = parsedLines[i];
            LoggedString nextline = i < parsedLines.Count - 1 ? parsedLines[i + 1] : null;

            if (currentLine.Event == "RESTART CHAPTER") {
                processed.Clear();
            }
            
            if (!currentLine.isCleared() || !currentLine.CountTowardsClear) {
                currentClip.Clear();
                continue;
            }

            currentClip.Add(previousLine);
            
            if (nextline?.isCleared() != true) {
                LoggedString clipEnd = currentLine;
                
                if (nextline?.Event == "INTER ROOM PASSED") {
                    currentClip.Add(currentLine);
                    clipEnd = currentClip.LastOrDefault(log => log.Room == nextline.Room && log.isCleared());
                }
                
                if (clipEnd != null) {
                    processed.Add([currentClip[0], clipEnd]);
                }
            }
        }
        return processed;
    }

    public static void ProcessLastLogFromState() {
        if (!Directory.Exists(VidcutterModule.Settings.VideoFolder)) {
            Tooltip.Show(Dialog.Clean("VIDCUTTER_TOOLTIP_VIDEO_FOLDER_NOT_FOUND"));
            return;
        }
        string lastVideo = Directory.GetFiles(VidcutterModule.Settings.VideoFolder)
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .FirstOrDefault();
        if (lastVideo == null) {
            Tooltip.Show(Dialog.Clean("VIDCUTTER_TOOLTIP_VIDEO_NOT_FOUND"));
            return;
        }
        TimeSpan? duration = getVideoDuration(lastVideo, out bool isFinished);
        if (duration == null) {
            Tooltip.Show(Dialog.Clean("VIDCUTTER_TOOLTIP_NO_MATROSKA"));
            return;
        }
        List<LoggedString> logs = VidcutterModule.getAllLogs(lastVideo);
        LoggedString stateLog = logs.LastOrDefault(log => log.Event.Contains("STATE"));
        if (stateLog == null) {
            Tooltip.Show(Dialog.Clean("VIDCUTTER_TOOLTIP_STATE_NOT_FOUND"));
            return;
        }
        LoggedString endLog = logs.LastOrDefault();
        
        TooltipWithProgress progress = TooltipWithProgress.Show(Dialog.Clean("VIDCUTTER_TOOLTIP_PROCESSING_VIDEO"));
        
        void process() => ProcessLastLogFromState(progress, lastVideo, stateLog, endLog);
        if (!isFinished) {
            progress.AddLoadingDelay(5f, process);
        } else {
            process();
        }
    }

    public static void ProcessLastLogFromState(TooltipWithProgress progress, string lastVideo, LoggedString stateLog, LoggedString endLog) {
        DateTime videoStartTime = File.GetCreationTime(lastVideo);
        TimeSpan startTime = stateLog.Time + TimeSpan.FromSeconds(VidcutterModule.Settings.DelayStart) - videoStartTime;
        float delay = VidcutterModule.Settings.DelayEnd;
        TimeSpan endTime = endLog.Time + TimeSpan.FromSeconds(delay) - videoStartTime;
        string ss = $"{startTime:hh\\:mm\\:ss\\.fff}";
        string to = $"{endTime:hh\\:mm\\:ss\\.fff}";
        string output = getOutputVideoName(stateLog.Level);
        double clipDuration = (endTime - startTime).TotalSeconds;
        Process process = createProcess($"{VidcutterModule.Settings.FFmpegPath}ffmpeg", $"-ss {ss} -to {to} -i \"{lastVideo}\" -c:a copy -map 0 -vcodec libx264 " +
                                $"-crf {VidcutterModule.Settings.CRF} -preset veryfast -y {output} -v warning -progress pipe:1");
        process.OutputDataReceived += (sender, e) => {
            if (e.Data?.StartsWith("out_time=") ?? false) {
                string[] splitted = e.Data.Split('=');
                if (TimeSpan.TryParse(splitted[1], out TimeSpan currentTime)) {
                    progress.progress = (float) (currentTime.TotalSeconds / clipDuration);
                }
            }
        };
        process.EnableRaisingEvents = true;
        process.Exited += (sender, e) => {
            progress.progress = 1f;
            Tooltip.Show(output + " " + Dialog.Clean("VIDCUTTER_TOOLTIP_PROCESSED_VIDEO"), 3f);
        };
        process.Start();
        process.BeginOutputReadLine();
    }
}
