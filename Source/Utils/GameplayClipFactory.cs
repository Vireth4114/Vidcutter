using Celeste.Mod.Vidcutter.Models;

namespace Celeste.Mod.Vidcutter.Utils;

public class GameplayClipFactory(ClipDelays delays) {
    public GameplayClip Create(LoggedString start, LoggedString end) {
        return new GameplayClip(
            start,
            end,
            delays.Start,
            end.Event == "LEVEL COMPLETE" ? delays.Complete : delays.End
        );
    }
}