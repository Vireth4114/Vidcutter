using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Vidcutter.Models;

namespace Celeste.Mod.Vidcutter.Services.Logs;

public static class LogService {
    private static LogReader _reader;
    private static LogWriter _writer;

    public static void SwitchToReader() {
        if (_reader != null) return;
        _reader = new CachedLogReader();
        _writer?.Dispose();
        _writer = null;
    }

    public static void SwitchToWriter() {
        if (_writer != null) return;
        _writer = new LogWriter();
        _reader = null;
    }

    public static void Close() {
        _writer?.Dispose();
        _writer = null;
        _reader = null;
    }

    public static List<LoggedString> GetAllLogs() {
        if (_reader == null) throw new Exception("Reader not initialized");
        return _reader.GetAllLogs();
    }

    public static List<LoggedString> GetAllLogs(VideoFile video, string level = null) {
        return GetAllLogs().Where(log =>
            video.CreationTime <= log.Time &&
            log.Time <= video.EndTime &&
            (level == null || log.Level == level)
        ).ToList();
    }
    
    public static void DeleteLogs(List<LevelInAVideo> rows) {
        if (_reader == null) throw new Exception("Reader not initialized");
        List<LoggedString> allLogs = _reader.GetAllLogs();
        
        foreach (LevelInAVideo row in rows)
            allLogs.RemoveAll(row.HasLog);
        
        using LogWriter writer = new(append: false);
        writer.WriteLogs(allLogs);
    }

    public static void Log(string message, Session session, VidcutterState state) {
        if (_writer == null) throw new Exception("Writer not initialized");
        LoggedString log = LoggedString.GetFromSession(message, session);
        
        if (!state.IsFromASavestate && log.IsCleared()) {
            if (state.LastEvent != null && !state.LastEvent.IsCleared()) {
                _writer.WriteLog(state.LastEvent);
            } 
            _writer.WriteLog(log);
        }

        state.LastEvent = log;
    }
}