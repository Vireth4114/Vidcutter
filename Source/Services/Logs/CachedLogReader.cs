using System.Collections.Generic;
using Celeste.Mod.Vidcutter.Models;

namespace Celeste.Mod.Vidcutter.Services.Logs;

public class CachedLogReader: LogReader {
    private readonly SimpleLogReader _reader = new();
    private List<LoggedString> _logs;
    
    public override List<LoggedString> GetAllLogs() {
        _logs ??= _reader.GetAllLogs();
        return _logs;
    }
}