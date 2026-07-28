using Celeste.Mod.Vidcutter.Utils;

namespace Celeste.Mod.Vidcutter;

public record LevelInAVideo(string VideoName, string Level) {
    public VideoFile Video => VideoFile.Get(VideoName);
}