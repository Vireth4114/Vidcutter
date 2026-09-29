using Microsoft.Xna.Framework;

namespace Celeste.Mod.Vidcutter.Models;

public class LoggingState {
    public Vector2? PreviousRespawnPoint { get; set; }
    public bool LogWhenCloseToSpawnPoint { get; set; }
    public bool IsFromASavestate { get; set; }
    public LoggedString LastEvent { get; set; }
    public LoggedString LastState { get; set; }
}