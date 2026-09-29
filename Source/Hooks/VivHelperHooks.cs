using System;
using System.Reflection;
using Celeste.Mod.Vidcutter.Models;
using Celeste.Mod.Vidcutter.Services.Logs;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.Vidcutter.Hooks;

public class VivHelperHooks(LoggingState state) : IHook {
    private static LoggingState _state;
    
    private EverestModule _vivHelperModule;
    private Hook _segmentedRoomPassedHook;
    private Assembly _vivHelperAsm;

    private static Level OnPassingSegmentedRoom(Func<Level, Level> orig, Level level) {
        Vector2? respawnPoint = level.Session.RespawnPoint;
        Level returnValue = orig(level);
        Vector2? newRespawnPoint = returnValue.Session.RespawnPoint;
        if (respawnPoint != newRespawnPoint) {
            LogService.Log($"BACK TO START OF INTER ROOM", level.Session, _state);
            _state.PreviousRespawnPoint = newRespawnPoint;
        }
        return returnValue;
    }

    private void LoadPassingSegmentedRoomHook() {
        if (_vivHelperAsm == null)
            return;
        
        MethodInfo target = _vivHelperAsm.GetType("VivHelper.Entities.SpawnPointHooks")?.GetMethod(
            "ModifyRoomToRespawnTo",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        if (target == null) {
            Logger.Info("Vidcutter", "VivHelper's ModifyRoomToRespawnTo method not found, skipping hook installation");
            return;
        }

        MethodInfo hookInfo = typeof(VivHelperHooks).GetMethod(
            nameof(OnPassingSegmentedRoom),
            BindingFlags.NonPublic | BindingFlags.Static
        ) ?? throw new MissingMethodException("OnPassingSegmentedRoom method not found in VivHelperHooks");

        _segmentedRoomPassedHook = new Hook(target, hookInfo);
    }
    
    public void Load() {
        _state = state;
        EverestModuleMetadata vivHelper = new() {
            Name = "VivHelper",
            Version = new Version(1, 14, 0)
        };

        Everest.Loader.TryGetDependency(vivHelper, out _vivHelperModule);
        
        if (_vivHelperModule == null) {
            Logger.Info("Vidcutter", "VivHelper not found, skipping hook installation");
            return;
        }

        _vivHelperAsm = _vivHelperModule.GetType().Assembly;

        LoadPassingSegmentedRoomHook();
    }

    public void Unload() {
        _segmentedRoomPassedHook?.Dispose();
        _segmentedRoomPassedHook = null;
    }
}