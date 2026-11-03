using Celeste.Mod.Vidcutter.Models;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Vidcutter.Hooks;

public class HookState {
    public Vector2? PreviousRespawnPoint { get; set; }
    public bool LogWhenCloseToSpawnPoint { get; set; }
    public bool IsFromASavestate { get; set; }
    public LoggedString LastEvent { get; set; }
    public LoggedString LastState { get; set; }
}