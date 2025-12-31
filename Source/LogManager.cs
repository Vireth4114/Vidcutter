using System;
using System.Collections.Generic;
using System.IO;

namespace Celeste.Mod.Vidcutter;

class LogManager {
    public static string logPath;
    public static StreamWriter LogFileWriter = null;

    public static void Log(string message, Session session = null) {
        string toLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ";
        if (session != null) {
            string sid = session.Area.SID;
            if (sid.StartsWith("Celeste/")) {
                sid = $"AREA_{sid.Substring(8, 1)}";
                if (sid == "AREA_L") {
                    sid = "AREA_10";
                }
            }
            toLog += Dialog.Clean(sid);
            if (session.Area.Mode.ToString().EndsWith("Side")) {
                toLog += $" [{session.Area.Mode.ToString()[0]}-Side]";
            }
            toLog += $" | {session.Level} | ";
        }
        toLog += message;
        LogFileWriter.WriteLine(toLog);
    }

    public static List<LoggedString> getAllLogs(string video, string level = null) {
        DateTime startVideo = File.GetCreationTime(video);
        TimeSpan? duration = VideoCreation.getVideoDuration(video);
        Logger.Info("Vidcutter", $"Video {video} started at {startVideo} and has duration {duration}");
        if (duration == null) {
            return new List<LoggedString>();
        }
        DateTime endVideo = startVideo + (TimeSpan)duration;
        return getAllLogs(startVideo, endVideo, level);
    }

    public static List<LoggedString> getAllLogs(DateTime? startVideo = null, DateTime? endVideo = null, string level = null) {
        LogFileWriter.Close();
        string[] lines = File.ReadAllLines(logPath);
        List<LoggedString> parsedLines = new List<LoggedString>();
        foreach (string line in lines) {
            DateTime logTime = DateTime.Parse(line.Substring(1, 23));
            string[] loggedEvent = line.Substring(26).Split(" | ");
            bool condition = true;
            if (startVideo != null) {
                condition &= startVideo <= logTime;
            }
            if (endVideo != null) {
                condition &= logTime <= endVideo;
            }
            if (level != null) {
                condition &= loggedEvent[0] == level;
            }
            if (condition) {
                parsedLines.Add(new LoggedString(logTime, loggedEvent[2], loggedEvent[0], loggedEvent[1]));
            }
        }

        LogFileWriter = new StreamWriter(logPath, true) {
            AutoFlush = true
        };
        return parsedLines;
    }

    public static void deleteLogs(List<ProcessedVideo> rows){
        List<LoggedString> allLogs = getAllLogs();
        foreach (ProcessedVideo row in rows) {
            string video = Path.Combine(VidcutterModule.Settings.VideoFolder, row.Video);
            string level = row.Level;
            DateTime startVideo = File.GetCreationTime(video);
            TimeSpan? duration = VideoCreation.getVideoDuration(video);
            if (duration == null) {
                return;
            }
            DateTime endVideo = startVideo + (TimeSpan)duration;
            List<LoggedString> allLogsCopy = [.. allLogs];
            foreach (LoggedString log in allLogsCopy) {
                if (startVideo < log.Time && log.Time < endVideo && log.Level == level) {
                    allLogs.Remove(log);
                }
            }
        }

        LogFileWriter.Close();
        using (StreamWriter writer = new StreamWriter(logPath, false)) {
            foreach (LoggedString log in allLogs) {
                writer.WriteLine(log.ToString());
            }
        }
        LogFileWriter = new StreamWriter(logPath, true) {
            AutoFlush = true
        };
    }
}