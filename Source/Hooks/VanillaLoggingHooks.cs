using System;
using System.Collections;
using Celeste.Mod.Vidcutter.Models;
using Celeste.Mod.Vidcutter.Services.Logs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Vidcutter.Hooks;

public class VanillaLoggingHooks(LoggingState state) : IHook {
    private static LoggingState _state;
    
    private static void OnComplete(Level level) {
        Log("LEVEL COMPLETE", session: level.Session);
        _state.LogWhenCloseToSpawnPoint = false;
    }

    private static void OnDeath(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader = false) {
        if (Engine.Scene is Level && playerIntro == Player.IntroTypes.Respawn) {
            _state.IsFromASavestate = false;
            if (_state.LastEvent != null && _state.LastEvent.IsCollectable()) {
                Log("DEATH AFTER COLLECTIBLE", session: self.Session);
            } else {
                Log("DEATH", session: self.Session);
            }
            _state.LogWhenCloseToSpawnPoint = false;
        }
        orig(self, playerIntro, isFromLoader);
    }

    private static void OnBegin(On.Celeste.Level.orig_Begin orig, Level self) {
        _state.IsFromASavestate = false;
        orig(self);
    }

    private static void OnCollectStrawberry(On.Celeste.Strawberry.orig_OnCollect orig, Strawberry self) {
        Log("BERRY", session: self.SceneAs<Level>().Session);
        orig(self);
    }

    private static void OnCollectCassette(On.Celeste.Cassette.orig_OnPlayer orig, Cassette self, Player player) {
        if (!self.collected)
            Log("CASSETTE", session: self.SceneAs<Level>().Session);
        orig(self, player);
    }

    private static void OnRestart(On.Celeste.LevelExit.orig_ctor orig, LevelExit self, LevelExit.Mode mode, Session session, HiresSnow snow) {
        if (mode == LevelExit.Mode.Restart) {
            Log("RESTART CHAPTER", session: session);
        }
        orig(self, mode, session, snow);
    }

    private static void OnCollectHeartGem(On.Celeste.HeartGem.orig_Collect orig, HeartGem self, Player player) {
        Log("HEART", session: self.SceneAs<Level>().Session);
        orig(self, player);
    }

    private static void OnCollectKey(On.Celeste.Key.orig_OnPlayer orig, Key self, Player player) {
        if (self.GetType() == typeof(Key) && self.Collidable)
            Log("KEY", session: self.SceneAs<Level>().Session);
        orig(self, player);
    }

    private static IEnumerator OnCollectSummitGem(On.Celeste.SummitGem.orig_SmashRoutine orig, SummitGem self, Player player, Level level) {
        Log("SUMMIT_GEM", session: level.Session);
        return orig(self, player, level);
    }

    private static void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self) {
        orig(self);
        Vector2 playerPos = self.Position;
        Vector2? respawnPoint = self.SceneAs<Level>().Session.RespawnPoint;
        if (respawnPoint == null) {
            return;
        }
        if (_state.PreviousRespawnPoint != respawnPoint) {
            _state.PreviousRespawnPoint = respawnPoint;
            Log($"ROOM PASSED", session: self.SceneAs<Level>().Session);
            _state.LogWhenCloseToSpawnPoint = true;
        }
        float deltaY = Math.Abs(playerPos.Y - respawnPoint.Value.Y);
        float deltaX = Math.Abs(playerPos.X - respawnPoint.Value.X);
        double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        if (distance <= 50 && _state.LogWhenCloseToSpawnPoint) {
            if ((DateTime.Now - _state.LastEvent.Time).TotalSeconds > 0.1) {
                Log($"CLOSE TO SPAWNPOINT", session: self.SceneAs<Level>().Session);
            }
            _state.LogWhenCloseToSpawnPoint = false;
        }
    }

    private static void Log(string message, Session session) {
        LogService.Log(message, session, _state);
    }
    
    public void Load() {
        _state = state;
        Everest.Events.Level.OnComplete += OnComplete;
        On.Celeste.Level.Begin += OnBegin;
        On.Celeste.Level.LoadLevel += OnDeath;
        On.Celeste.Player.Update += OnPlayerUpdate;
        On.Celeste.Strawberry.OnCollect += OnCollectStrawberry;
        On.Celeste.Cassette.OnPlayer += OnCollectCassette;
        On.Celeste.LevelExit.ctor += OnRestart;
        On.Celeste.HeartGem.Collect += OnCollectHeartGem;
        On.Celeste.Key.OnPlayer += OnCollectKey;
        On.Celeste.SummitGem.SmashRoutine += OnCollectSummitGem;
    }

    public void Unload() {
        Everest.Events.Level.OnComplete -= OnComplete;
        On.Celeste.Level.Begin -= OnBegin;
        On.Celeste.Level.LoadLevel -= OnDeath;
        On.Celeste.Player.Update -= OnPlayerUpdate;
        On.Celeste.Strawberry.OnCollect -= OnCollectStrawberry;
        On.Celeste.Cassette.OnPlayer -= OnCollectCassette;
        On.Celeste.LevelExit.ctor -= OnRestart;
        On.Celeste.HeartGem.Collect -= OnCollectHeartGem;
        On.Celeste.Key.OnPlayer -= OnCollectKey;
        On.Celeste.SummitGem.SmashRoutine -= OnCollectSummitGem;
    }
}