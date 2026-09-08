namespace Celeste.Mod.Vidcutter.Models;

public record LevelInAVideo(string VideoName, string Level) {
    public VideoFile Video => VideoFile.Get(VideoName);
    
    public LoggedString LastLog { get; set; }
}