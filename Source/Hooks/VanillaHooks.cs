using System;
using System.Collections;
using Celeste.Mod.Vidcutter.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Vidcutter.Hooks;

public static class VanillaHooks {
    private static VidcutterState State => VidcutterModule.State;
    private static VidcutterModuleSettings Settings => VidcutterModule.Settings;
    
    private static void OnComplete(Level level, Scene nextScene, ref bool shouldReloadPortraits, ref bool shouldDissociateEntities) {
        LogManager.Log("LEVEL COMPLETE", session: level.Session);
        State.LogWhenCloseToSpawnPoint = false;
    }

    private static void OnDeath(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader = false) {
        if (playerIntro == Player.IntroTypes.Respawn) {
            State.IsFromASavestate = false;
            LogManager.Log("DEATH", session: self.Session);
            State.LogWhenCloseToSpawnPoint = false;
        }
        orig(self, playerIntro, isFromLoader);
    }

    private static void OnBegin(On.Celeste.Level.orig_Begin orig, Level self) {
        State.IsFromASavestate = false;
        orig(self);
    }

    private static void OnCollectStrawberry(On.Celeste.Strawberry.orig_OnCollect orig, Strawberry self) {
        LogManager.Log("BERRY", session: self.SceneAs<Level>().Session);
        orig(self);
    }

    private static void OnCollectCassette(On.Celeste.Cassette.orig_OnPlayer orig, Cassette self, Player player) {
        if (!self.collected)
            LogManager.Log("CASSETTE", session: self.SceneAs<Level>().Session);
        orig(self, player);
    }

    private static void OnRestart(On.Celeste.LevelExit.orig_ctor orig, LevelExit self, LevelExit.Mode mode, Session session, HiresSnow snow) {
        if (mode == LevelExit.Mode.Restart) {
            LogManager.Log("RESTART CHAPTER", session: session);
        }
        orig(self, mode, session, snow);
    }

    private static void OnCollectHeartGem(On.Celeste.HeartGem.orig_Collect orig, HeartGem self, Player player) {
        LogManager.Log("HEART", session: self.SceneAs<Level>().Session);
        orig(self, player);
    }

    private static void OnCollectKey(On.Celeste.Key.orig_OnPlayer orig, Key self, Player player) {
        if (self.GetType() == typeof(Key) && self.Collidable)
            LogManager.Log("KEY", session: self.SceneAs<Level>().Session);
        orig(self, player);
    }

    private static IEnumerator OnCollectSummitGem(On.Celeste.SummitGem.orig_SmashRoutine orig, SummitGem self, Player player, Level level) {
        LogManager.Log("SUMMIT_GEM", session: level.Session);
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
            LogManager.Log($"ROOM PASSED", session: self.SceneAs<Level>().Session);
            State.LogWhenCloseToSpawnPoint = true;
        }
        float deltaY = Math.Abs(playerPos.Y - respawnPoint.Value.Y);
        float deltaX = Math.Abs(playerPos.X - respawnPoint.Value.X);
        double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        if (distance <= 50 && State.LogWhenCloseToSpawnPoint) {
            LogManager.Log($"CLOSE TO SPAWNPOINT", session: self.SceneAs<Level>().Session);
            State.LogWhenCloseToSpawnPoint = false;
        }
    }

    private static void OnUpdate(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime) {
        if (Settings.CutFromLastSaveState.Pressed) {
            VideoManager.ProcessLastLogFromStateWithTooltip();
        }
        orig(self, gameTime);
    }
    
    public static void Load() {
        Everest.Events.Level.OnEnd += OnComplete;
        On.Celeste.Level.Begin += OnBegin;
        On.Celeste.Level.LoadLevel += OnDeath;
        On.Celeste.Player.Update += OnPlayerUpdate;
        On.Monocle.Engine.Update += OnUpdate;
        On.Celeste.Strawberry.OnCollect += OnCollectStrawberry;
        On.Celeste.Cassette.OnPlayer += OnCollectCassette;
        On.Celeste.LevelExit.ctor += OnRestart;
        On.Celeste.HeartGem.Collect += OnCollectHeartGem;
        On.Celeste.Key.OnPlayer += OnCollectKey;
        On.Celeste.SummitGem.SmashRoutine += OnCollectSummitGem;
    }

    public static void Unload() {
        Everest.Events.Level.OnEnd -= OnComplete;
        On.Celeste.Level.Begin -= OnBegin;
        On.Celeste.Level.LoadLevel -= OnDeath;
        On.Celeste.Player.Update -= OnPlayerUpdate;
        On.Monocle.Engine.Update -= OnUpdate;
        On.Celeste.Strawberry.OnCollect -= OnCollectStrawberry;
        On.Celeste.Cassette.OnPlayer -= OnCollectCassette;
        On.Celeste.LevelExit.ctor -= OnRestart;
        On.Celeste.HeartGem.Collect -= OnCollectHeartGem;
        On.Celeste.Key.OnPlayer -= OnCollectKey;
        On.Celeste.SummitGem.SmashRoutine -= OnCollectSummitGem;
    }
}