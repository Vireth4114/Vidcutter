using System.Collections.Generic;
using Celeste.Mod.Vidcutter.Models;

namespace Celeste.Mod.Vidcutter.Services.Logs;

public interface ILogReader {
    public List<LoggedString> GetAllLogs();
}