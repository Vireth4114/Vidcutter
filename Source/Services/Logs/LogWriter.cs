using System;
using System.Collections.Generic;
using System.IO;
using Celeste.Mod.Vidcutter.Models;
using static Celeste.Mod.Vidcutter.Utils.FileConstants;

namespace Celeste.Mod.Vidcutter.Services.Logs;

public class LogWriter(bool append = true): IDisposable {
    private StreamWriter _logFileWriter = new(LogFile, append) {
        AutoFlush = true
    };

    public void Dispose() {
        _logFileWriter?.Dispose();
        _logFileWriter = null;
    }

    public void WriteLog(LoggedString log) {
        _logFileWriter?.WriteLine(log.ToString());
    }

    public void WriteLogs(List<LoggedString> logs) {
        foreach (LoggedString log in logs)
            WriteLog(log);
    }
}