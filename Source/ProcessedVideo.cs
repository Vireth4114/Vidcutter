namespace Celeste.Mod.Vidcutter;

public class ProcessedVideo {
    public string Video { get; set; }
    public string Level { get; set; }

    public ProcessedVideo(string video, string level) {
        Video = video;
        Level = level;
    }
}