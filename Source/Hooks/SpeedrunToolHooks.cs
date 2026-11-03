using System;
using System.Collections.Generic;
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

public class SpeedrunToolHooks : IHook {
    private static HookState State => HookManager.State;
    
    private bool _speedrunToolInstalled;
    private object _saveLoadActionRegistered;

    private static void OnLoadState(Level level) {
        Vector2? playerPosition = level.Tracker.GetEntity<Player>()?.Position;
        if (playerPosition == level.Session.RespawnPoint) {
            HookManager.Log("STATE ON RESPAWN POINT", level.Session);
        } else {   
            HookManager.Log("STATE", level.Session);
            State.IsFromASavestate = true;
        }
        State.LastState = State.LastEvent;
        State.LogWhenCloseToSpawnPoint = false;
        State.PreviousRespawnPoint = level.Session.RespawnPoint;
    }

    public void Load() {
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

    public void Unload() {
        if (_speedrunToolInstalled) {
            SpeedrunToolImport.Unregister(_saveLoadActionRegistered);
        }
    }
}