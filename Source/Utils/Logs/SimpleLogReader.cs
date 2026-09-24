using System.Collections.Generic;
using System.IO;
using System.Linq;
using Celeste.Mod.Vidcutter.Models;
using static Celeste.Mod.Vidcutter.Utils.FileConstants;

namespace Celeste.Mod.Vidcutter.Utils.Logs;

public class SimpleLogReader: LogReader {
    public override List<LoggedString> GetAllLogs() {
        return File.ReadAllLines(LogFile)
            .Select(LoggedString.Parse)
            .ToList();
    }
}