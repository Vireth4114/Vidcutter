using System.Collections.Generic;
using Celeste.Mod.Vidcutter.Models;

namespace Celeste.Mod.Vidcutter.Services.Logs;

public abstract class LogReader {
    public abstract List<LoggedString> GetAllLogs();
}