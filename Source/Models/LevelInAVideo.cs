using System.IO;

namespace Celeste.Mod.Vidcutter.Models;

public record LevelInAVideo(string VideoPath, string Level) {
    public string VideoName => Path.GetFileName(VideoPath);
    public LoggedString FirstLog { get; init; }
    public LoggedString LastLog { get; init; }
    
    public bool HasLog(LoggedString log) {
        return log.Level == Level && log.Time >= FirstLog.Time && log.Time <= LastLog.Time;
    }
}