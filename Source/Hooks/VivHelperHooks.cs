using System;
using System.Reflection;
using Celeste.Mod.Vidcutter.Utils;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.Vidcutter.Hooks;

public static class VivHelperHooks {
    private static VidcutterState State => VidcutterModule.State;
    
    private static EverestModule _vivHelperModule;
    private static Hook _segmentedRoomPassedHook;
    private static Assembly _vivHelperAsm;

    private static Level OnPassingSegmentedRoom(Func<Level, Level> orig, Level level) {
        Vector2? respawnPoint = level.Session.RespawnPoint;
        Level returnValue = orig(level);
        Vector2? newRespawnPoint = returnValue.Session.RespawnPoint;
        if (respawnPoint != newRespawnPoint) {
            LogManager.Log($"INTER ROOM PASSED", session: level.Session);
            State.PreviousRespawnPoint = newRespawnPoint;
        }
        return returnValue;
    }

    private static void LoadPassingSegmentedRoomHook() {
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
    
    public static void Load() {
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

    public static void Unload() {
        _segmentedRoomPassedHook?.Dispose();
        _segmentedRoomPassedHook = null;
    }
}