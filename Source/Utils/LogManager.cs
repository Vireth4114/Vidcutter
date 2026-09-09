using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Celeste.Mod.Vidcutter.Models;
using static Celeste.Mod.Vidcutter.Utils.FileUtils;

namespace Celeste.Mod.Vidcutter.Utils;

public static class LogManager {
    private static VidcutterState State => VidcutterModule.State;
    private static StreamWriter _logFileWriter;

    private static string LogFile {
        get {
            string logFolder = Path.Combine(VidcutterWorkingDirectory, Path.Combine("logs"));
            if (!Directory.Exists(logFolder)) {
                Directory.CreateDirectory(logFolder);
            }
            return Path.Combine(logFolder, "log.txt");
        }
    }

    public static void OpenWriter() {
        _logFileWriter = new StreamWriter(LogFile, true) {
            AutoFlush = true
        };
    }

    public static void CloseWriter() {
        _logFileWriter?.Dispose();
        _logFileWriter = null;
    }

    private static void WriteLine(string line) {
        if (_logFileWriter == null) return;
        _logFileWriter.WriteLine(line);
    }

    private static string ToTitleCase(string text) {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
    }

    public static void Log(string message, Session session) {
        LoggedString log = GetLog(message, session);
        
        if (!State.IsFromASavestate && log.IsCleared()) {
            if (State.LastEvent != null && !State.LastEvent.IsCleared()) {
                WriteLine(State.LastEvent.ToString());
            } 
            WriteLine(log.ToString());
        }

        State.LastEvent = log;
    }

    private static LoggedString GetLog(string message, Session session) {
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
        
        return new(DateTime.Now, message, chapter, room);
    }

    public static List<LoggedString> GetAllLogs(VideoFile video, string level = null) {
        return GetAllLogs(video.CreationTime, video.EndTime, level);
    }

    public static List<LoggedString> GetAllLogs(DateTime? startVideo = null, DateTime? endVideo = null, string level = null) {
        return File.ReadAllLines(LogFile)
            .Select(LoggedString.Parse)
            .Where(log =>
                (startVideo == null || startVideo <= log.Time) &&
                (endVideo == null || log.Time <= endVideo) &&
                (level == null || log.Level == level)
            ).ToList();
    }

    public static void DeleteLogs(List<LevelInAVideo> rows){
        List<LoggedString> allLogs = GetAllLogs();
        
        foreach (LevelInAVideo row in rows) {
            allLogs.RemoveAll(row.HasLog);
        }

        using StreamWriter writer = new StreamWriter(LogFile, false);
        foreach (LoggedString log in allLogs)
            writer.WriteLine(log.ToString());
    }
}