using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Vidcutter.Models;

namespace Celeste.Mod.Vidcutter.Services.Logs;

public static class LogService {
    private static ILogReader Reader { get; set; }
    private static LogWriter Writer { get; set; }

    public static void SwitchToReader() {
        if (Reader != null) return;
        Reader = new CachedLogReader();
        Writer?.Dispose();
        Writer = null;
    }

    public static void SwitchToWriter() {
        if (Writer != null) return;
        Writer = new LogWriter();
        Reader = null;
    }

    public static void Close() {
        Writer?.Dispose();
        Writer = null;
        Reader = null;
    }

    public static List<LoggedString> GetAllLogs() {
        if (Reader == null) throw new InvalidOperationException("Reader not initialized");
        return Reader.GetAllLogs();
    }

    public static List<LoggedString> GetAllLogs(VideoFile video, string level = null) {
        return GetAllLogs().Where(log =>
            video.CreationTime <= log.Time &&
            log.Time <= video.EndTime &&
            (level == null || log.Level == level)
        ).ToList();
    }
    
    public static void DeleteLogs(List<LevelInAVideo> rows) {
        if (Reader == null) throw new InvalidOperationException("Reader not initialized");
        List<LoggedString> allLogs = Reader.GetAllLogs();
        
        foreach (LevelInAVideo row in rows)
            allLogs.RemoveAll(row.HasLog);
        
        using LogWriter writer = new(append: false);
        writer.WriteLogs(allLogs);
    }

    public static void WriteLog(LoggedString log) {
        if (Writer == null) throw new InvalidOperationException("Writer not initialized");
        Writer.WriteLog(log);
    }
}