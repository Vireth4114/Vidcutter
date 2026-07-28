using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Celeste.Mod.Vidcutter.Models;

namespace Celeste.Mod.Vidcutter.Utils;

public static class LogManager {
    private static VidcutterState State => VidcutterModule.State;
    private static StreamWriter _logFileWriter;
    private static bool _initialized;

    public static void Initialize() {
        string logFolder = Path.Combine(FileUtils.VidcutterWorkingDirectory, Path.Combine("logs"));
        if (!Directory.Exists(logFolder)) {
            Directory.CreateDirectory(logFolder);
        }
        OpenWriter();
        _initialized = true;
    }

    public static void OpenWriter() {
        _logFileWriter = new StreamWriter(FileUtils.LogFile, true) {
            AutoFlush = true
        };
    }

    public static void CloseWriter() {
        _logFileWriter?.Dispose();
        _logFileWriter = null;
    }

    private static void WriteLine(string line) {
        if (!_initialized) Initialize();
        _logFileWriter.WriteLine(line);
    }

    private static string[] GetAllLinesFromLogFile() {
        CloseWriter();
        string[] lines = File.ReadAllLines(FileUtils.LogFile);
        OpenWriter();
        return lines;
    }

    private static void RewriteLogFileWith(string[] lines) {
        CloseWriter();
        using (StreamWriter writer = new StreamWriter(FileUtils.LogFile, false)) {
            foreach (string line in lines)
                writer.WriteLine(line);
        }
        OpenWriter();
    }
    
    private static string ToTitleCase(string text) {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
    }

    public static void Log(string message, Session session) {
        string sid = session.Area.SID;
        if (sid.StartsWith("Celeste/")) {
            sid = sid.Contains("LostLevels") ? "AREA_10" : $"AREA_{sid[8]}";
        }
        
        string chapter = Dialog.Clean(sid).Replace("|", "-");

        chapter += session.Area.Mode switch {
            AreaMode.BSide => $" [{ToTitleCase(Dialog.Clean("OVERWORLD_REMIX"))}]",
            AreaMode.CSide => $" [{ToTitleCase(Dialog.Clean("OVERWORLD_REMIX2"))}]",
            _ => ""
        };

        string room = session.Level.Replace("|", "-");
        
        WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {chapter} | {room} | {message} | {!State.IsFromASavestate}");
    }

    public static List<LoggedString> GetAllLogs(VideoFile video, string level = null) {
        return GetAllLogs(video.GetCreationTime(), video.GetEndTime(), level);
    }

    public static List<LoggedString> GetAllLogs(DateTime? startVideo = null, DateTime? endVideo = null, string level = null) {
        string[] lines = GetAllLinesFromLogFile();
        
        List<LoggedString> parsedLines = new List<LoggedString>();
        foreach (string line in lines) {
            DateTime logTime = DateTime.Parse(line[1..24]);
            string[] loggedEvent = line[26..].Split(" | ");
                
            if ((startVideo == null || startVideo <= logTime) &&
                (endVideo == null || logTime <= endVideo) &&
                (level == null || loggedEvent[0] == level))
            {
                bool countTowardsClear = loggedEvent.ElementAtOrDefault(3) == null || bool.Parse(loggedEvent[3]);
                parsedLines.Add(new LoggedString(logTime, loggedEvent[2], loggedEvent[0], loggedEvent[1], countTowardsClear));
            }
        }
        return parsedLines;
    }

    public static void DeleteLogs(List<LevelInAVideo> rows){
        List<LoggedString> allLogs = GetAllLogs();
        
        foreach (LevelInAVideo row in rows) {
            VideoFile video = VideoFile.Get(row.VideoName);
            
            string level = row.Level;
            DateTime startVideo = video.GetCreationTime();
            DateTime endVideo = video.GetEndTime();
            
            allLogs.RemoveAll(log =>
                startVideo < log.Time &&
                log.Time < endVideo &&
                log.Level == level
            );
        }
        
        RewriteLogFileWith(allLogs.Select(log => log.ToString()).ToArray());
    }
}