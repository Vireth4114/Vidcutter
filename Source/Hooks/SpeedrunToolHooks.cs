using System;
using System.Collections.Generic;
using Celeste.Mod.Vidcutter.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.ModInterop;

namespace Celeste.Mod.Vidcutter.Hooks;

[ModImportName("SpeedrunTool.SaveLoad")]
public static class SpeedrunToolImport {
    public static Func<
        Action<Dictionary<Type, Dictionary<string, object>>, Level>,
        Action<Dictionary<Type, Dictionary<string, object>>, Level>,
        Action,
        Action<Level>,
        Action<Level>,
        Action,
        object
    > RegisterSaveLoadAction;
    public static Action<Entity, bool> IgnoreSaveState;
    public static Action<object> Unregister;
}

public static class SpeedrunToolHooks {
    private static VidcutterState State => VidcutterModule.State;
    private static bool _speedrunToolInstalled;
    private static object _saveLoadActionRegistered;

    private static void OnLoadState(Level level) {
        Vector2? playerPosition = level.Tracker.GetEntity<Player>()?.Position;
        if (playerPosition == level.Session.RespawnPoint) {
            LogManager.Log("STATE ON RESPAWN POINT", session: level.Session);
        } else {   
            LogManager.Log("STATE", session: level.Session);
            State.IsFromASavestate = true;
        }
        State.LastState = State.LastEvent;
        State.LogWhenCloseToSpawnPoint = false;
        State.PreviousRespawnPoint = level.Session.RespawnPoint;
    }

    public static void Load() {
        typeof(SpeedrunToolImport).ModInterop();
        _speedrunToolInstalled = SpeedrunToolImport.IgnoreSaveState is not null;
        
        if (_speedrunToolInstalled) {
            _saveLoadActionRegistered = SpeedrunToolImport.RegisterSaveLoadAction(
                null,
                (_, level) => { OnLoadState(level); },
                null,
                null,
                null,
                null
            );
        }
    }

    public static void Unload() {
        if (_speedrunToolInstalled) {
            SpeedrunToolImport.Unregister(_saveLoadActionRegistered);
        }
    }
}