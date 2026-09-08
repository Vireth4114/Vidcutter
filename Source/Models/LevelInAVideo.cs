namespace Celeste.Mod.Vidcutter.Models;

public record LevelInAVideo(string VideoName, string Level) {
    public LoggedString LastLog { get; set; }
}