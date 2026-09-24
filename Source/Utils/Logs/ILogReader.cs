using System.Collections.Generic;
using Celeste.Mod.Vidcutter.Models;

namespace Celeste.Mod.Vidcutter.Utils.Logs;

public abstract class LogReader {
    public abstract List<LoggedString> GetAllLogs();
}