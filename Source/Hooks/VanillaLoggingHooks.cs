using System;
using System.Collections;
using Celeste.Mod.Vidcutter.Services.Logs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Vidcutter.Hooks;

public static class VanillaLoggingHooks {
    private static VidcutterState State => VidcutterState.Instance;
    
    private static void OnComplete(Level level) {
        LogService.Log("LEVEL COMPLETE", session: level.Session, state: State);
        State.LogWhenCloseToSpawnPoint = false;
    }

    private static void OnDeath(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader = false) {
        if (Engine.Scene is Level && playerIntro == Player.IntroTypes.Respawn) {
            State.IsFromASavestate = false;
            if (State.LastEvent != null && State.LastEvent.IsCollectable()) {
                LogService.Log("DEATH AFTER COLLECTIBLE", session: self.Session, state: State);
            } else {
                LogService.Log("DEATH", session: self.Session, state: State);
            }
            State.LogWhenCloseToSpawnPoint = false;
        }
        orig(self, playerIntro, isFromLoader);
    }

    private static void OnBegin(On.Celeste.Level.orig_Begin orig, Level self) {
        State.IsFromASavestate = false;
        orig(self);
    }

    private static void OnCollectStrawberry(On.Celeste.Strawberry.orig_OnCollect orig, Strawberry self) {
        LogService.Log("BERRY", session: self.SceneAs<Level>().Session, state: State);
        orig(self);
    }

    private static void OnCollectCassette(On.Celeste.Cassette.orig_OnPlayer orig, Cassette self, Player player) {
        if (!self.collected)
            LogService.Log("CASSETTE", session: self.SceneAs<Level>().Session, state: State);
        orig(self, player);
    }

    private static void OnRestart(On.Celeste.LevelExit.orig_ctor orig, LevelExit self, LevelExit.Mode mode, Session session, HiresSnow snow) {
        if (mode == LevelExit.Mode.Restart) {
            LogService.Log("RESTART CHAPTER", session: session, state: State);
        }
        orig(self, mode, session, snow);
    }

    private static void OnCollectHeartGem(On.Celeste.HeartGem.orig_Collect orig, HeartGem self, Player player) {
        LogService.Log("HEART", session: self.SceneAs<Level>().Session, state: State);
        orig(self, player);
    }

    private static void OnCollectKey(On.Celeste.Key.orig_OnPlayer orig, Key self, Player player) {
        if (self.GetType() == typeof(Key) && self.Collidable)
            LogService.Log("KEY", session: self.SceneAs<Level>().Session, state: State);
        orig(self, player);
    }

    private static IEnumerator OnCollectSummitGem(On.Celeste.SummitGem.orig_SmashRoutine orig, SummitGem self, Player player, Level level) {
        LogService.Log("SUMMIT_GEM", session: level.Session, state: State);
        return orig(self, player, level);
    }

    private static void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self) {
        orig(self);
        Vector2 playerPos = self.Position;
        Vector2? respawnPoint = self.SceneAs<Level>().Session.RespawnPoint;
        if (respawnPoint == null) {
            return;
        }
        if (State.PreviousRespawnPoint != respawnPoint) {
            State.PreviousRespawnPoint = respawnPoint;
            LogService.Log($"ROOM PASSED", session: self.SceneAs<Level>().Session, state: State);
            State.LogWhenCloseToSpawnPoint = true;
        }
        float deltaY = Math.Abs(playerPos.Y - respawnPoint.Value.Y);
        float deltaX = Math.Abs(playerPos.X - respawnPoint.Value.X);
        double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        if (distance <= 50 && State.LogWhenCloseToSpawnPoint) {
            if ((DateTime.Now - State.LastEvent.Time).TotalSeconds > 0.1) {
                LogService.Log($"CLOSE TO SPAWNPOINT", session: self.SceneAs<Level>().Session, state: State);
            }
            State.LogWhenCloseToSpawnPoint = false;
        }
    }
    
    public static void Load() {
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

    public static void Unload() {
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