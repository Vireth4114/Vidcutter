using System.Collections.Generic;
using Celeste.Mod.Vidcutter.Models;

namespace Celeste.Mod.Vidcutter.Services.Logs;

public class CachedLogReader: ILogReader {
    private readonly SimpleLogReader _reader = new();
    private List<LoggedString> _logs;
    
    public List<LoggedString> GetAllLogs() {
        _logs ??= _reader.GetAllLogs();
        return _logs;
    }
}