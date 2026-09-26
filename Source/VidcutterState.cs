using Celeste.Mod.Vidcutter.Models;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Vidcutter;

public class VidcutterState {
    private VidcutterState() {
        Instance = this;
    }
    public static VidcutterState Instance { get; private set; }

    public Vector2? PreviousRespawnPoint = null;
    public bool LogWhenCloseToSpawnPoint = false;
    public bool IsFromASavestate = false;
    public LoggedString LastEvent;
    public LoggedString LastState;
}