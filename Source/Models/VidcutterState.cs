using Celeste.Mod.Vidcutter.Models;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Vidcutter.Models;

public class VidcutterState {
    public Vector2? PreviousRespawnPoint = null;
    public bool LogWhenCloseToSpawnPoint = false;
    public bool IsFromASavestate = false;
    public LoggedString LastEvent;
    public LoggedString LastState;
}