using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Vidcutter.Hooks;

public class VanillaLoggingHooks : IHook {
    private static HookState State => HookManager.State;
    
    private static void OnComplete(Level level) {
        HookManager.Log("LEVEL COMPLETE", session: level.Session);
        State.LogWhenCloseToSpawnPoint = false;
    }

    private static void OnDeath(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader = false) {
        if (Engine.Scene is Level && playerIntro == Player.IntroTypes.Respawn) {
            State.IsFromASavestate = false;
            if (State.LastEvent != null && State.LastEvent.IsCollectable()) {
                HookManager.Log("DEATH AFTER COLLECTIBLE", session: self.Session);
            } else {
                HookManager.Log("DEATH", session: self.Session);
            }
            State.LogWhenCloseToSpawnPoint = false;
        }
        orig(self, playerIntro, isFromLoader);
    }

    private static void OnCollectStrawberry(On.Celeste.Strawberry.orig_OnCollect orig, Strawberry self) {
        HookManager.Log("BERRY", session: self.SceneAs<Level>().Session);
        orig(self);
    }

    private static void OnCollectCassette(On.Celeste.Cassette.orig_OnPlayer orig, Cassette self, Player player) {
        if (!self.collected)
            HookManager.Log("CASSETTE", session: self.SceneAs<Level>().Session);
        orig(self, player);
    }

    private static void OnRestart(On.Celeste.LevelExit.orig_ctor orig, LevelExit self, LevelExit.Mode mode, Session session, HiresSnow snow) {
        if (mode == LevelExit.Mode.Restart) {
            HookManager.Log("RESTART CHAPTER", session: session);
        }
        orig(self, mode, session, snow);
    }

    private static void OnCollectHeartGem(On.Celeste.HeartGem.orig_Collect orig, HeartGem self, Player player) {
        HookManager.Log("HEART", session: self.SceneAs<Level>().Session);
        orig(self, player);
    }

    private static void OnCollectKey(On.Celeste.Key.orig_OnPlayer orig, Key self, Player player) {
        if (self.GetType() == typeof(Key) && self.Collidable)
            HookManager.Log("KEY", session: self.SceneAs<Level>().Session);
        orig(self, player);
    }

    private static IEnumerator OnCollectSummitGem(On.Celeste.SummitGem.orig_SmashRoutine orig, SummitGem self, Player player, Level level) {
        HookManager.Log("SUMMIT_GEM", session: level.Session);
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
            HookManager.Log("ROOM PASSED", session: self.SceneAs<Level>().Session);
            State.LogWhenCloseToSpawnPoint = true;
        }
        if (Vector2.Distance(playerPos, respawnPoint.Value) <= 50 && State.LogWhenCloseToSpawnPoint) {
            if ((DateTime.Now - State.LastEvent.Time).TotalSeconds > 0.1) {
                HookManager.Log("CLOSE TO SPAWNPOINT", session: self.SceneAs<Level>().Session);
            }
            State.LogWhenCloseToSpawnPoint = false;
        }
    }
    
    public void Load() {
        Everest.Events.Level.OnComplete += OnComplete;
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