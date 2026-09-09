namespace Celeste.Mod.Vidcutter.Models;

public record LevelInAVideo(string VideoName, string Level) {
    public LoggedString FirstLog { get; init; }
    public LoggedString LastLog { get; init; }
    
    public bool HasLog(LoggedString log) {
        return log.Level == Level && log.Time >= FirstLog.Time && log.Time <= LastLog.Time;
    }
}