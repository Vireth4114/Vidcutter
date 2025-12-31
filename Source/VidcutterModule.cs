using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Monocle; 
using MonoMod.ModInterop;

namespace Celeste.Mod.Vidcutter;

[ModImportName("SpeedrunTool.SaveLoad")]
public static class VidcutterSpeedrunToolImport {
    public static Func<Action<Dictionary<Type, Dictionary<string, object>>, Level>, Action<Dictionary<Type, Dictionary<string, object>>, Level>, Action, Action<Level>, Action<Level>, Action, object> RegisterSaveLoadAction;
    public static Action<Entity, bool> IgnoreSaveState;
    public static Action<object> Unregister;
}

public class VidcutterModule : EverestModule {
    public static VidcutterModule Instance { get; private set; }

    public override Type SettingsType => typeof(VidcutterModuleSettings);
    public static VidcutterModuleSettings Settings => (VidcutterModuleSettings)Instance._Settings;

    public override Type SessionType => typeof(VidcutterModuleSession);
    public static VidcutterModuleSession Session => (VidcutterModuleSession)Instance._Session;

    public override Type SaveDataType => typeof(VidcutterModuleSaveData);
    public static VidcutterModuleSaveData SaveData => (VidcutterModuleSaveData)Instance._SaveData;

    public static Vector2? previousRespawnPoint = null;
    public static Level previousLevel = null;
    public static bool processWhenClose = false;
    private static bool SpeedrunToolInstalled = false;
    private static bool inState = false;
    private static object action;

    public static Dictionary<string, TimeSpan> DurationCache = null;

    public VidcutterModule() {
        Instance = this;
        Logger.SetLogLevel(nameof(VidcutterModule), LogLevel.Info);
    }

    public static void OnComplete(On.Celeste.Level.orig_RegisterAreaComplete orig, Level self) {
        if (!self.Completed) {
            LogManager.Log("LEVEL COMPLETE", session: self.Session);
            processWhenClose = false;
        }
        orig(self);
    }

    public static void OnDeath(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader = false) {
        if (playerIntro == Player.IntroTypes.Respawn) {
            LogManager.Log("DEATH", session: self.Session);
            processWhenClose = false;
            inState = false;
        }
        orig(self, playerIntro, isFromLoader);
    }

    public static void OnBegin(On.Celeste.Level.orig_Begin orig, Level self) {
        LogManager.Log("LEVEL LOADED", session: self.Session);
        inState = false;
        orig(self);
    }

    public static void OnCollectStrawberry(On.Celeste.Strawberry.orig_OnCollect orig, Strawberry self) {
        if (!inState) {
            LogManager.Log("ROOM PASSED", session: self.SceneAs<Level>().Session);
        }
        orig(self);
    }

    public static IEnumerator OnCollectCassette(On.Celeste.Cassette.orig_CollectRoutine orig, Cassette self, Player player) {
        yield return new SwapImmediately(orig(self, player));
        if (!inState) {
            LogManager.Log("ROOM PASSED", session: self.SceneAs<Level>().Session);
        }
    }

    public static void OnRestart(On.Celeste.LevelExit.orig_ctor orig, LevelExit self, LevelExit.Mode mode, Session session, HiresSnow snow) {
        if (mode == LevelExit.Mode.Restart) {
            LogManager.Log("RESTART CHAPTER", session: session);
        }
        orig(self, mode, session, snow);
    }

    public static void onPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self) {
        orig(self);
        Vector2 playerPos = self.Position;
        Vector2? respawnPoint = self.SceneAs<Level>().Session.RespawnPoint;
        if (respawnPoint == null) {
            return;
        }
        if (previousRespawnPoint != respawnPoint || previousLevel != self.SceneAs<Level>()) {
            previousLevel = self.SceneAs<Level>();
            previousRespawnPoint = respawnPoint;
            LogManager.Log($"ROOM PASSED", session: self.SceneAs<Level>().Session);
            processWhenClose = true;
        }
        float deltaY = Math.Abs(playerPos.Y - respawnPoint.Value.Y);
        float deltaX = Math.Abs(playerPos.X - respawnPoint.Value.X);
        double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        if (distance <= 50 && processWhenClose && !inState) {
            LogManager.Log($"ROOM PASSED", session: self.SceneAs<Level>().Session);
            processWhenClose = false;
        }
    }

    public static void onLoadState(Level level) {
        Vector2? playerPosition = level.Tracker.GetEntity<Player>()?.Position;
        if (playerPosition == level.Session.RespawnPoint) {
            LogManager.Log("DEATH", session: level.Session);
        } else {   
            LogManager.Log("STATE", session: level.Session);
        }
        processWhenClose = false;
        previousRespawnPoint = level.Session.RespawnPoint;
        previousLevel = level;
        inState = true;
    }

    public static bool InstallFFmpeg(OuiLoggedProgress progress) {
        string DownloadURL = "https://github.com/GyanD/codexffmpeg/releases/download/7.1/ffmpeg-7.1-essentials_build.zip";
        string DownloadFolder = Path.Combine("./VidCutter/", "ffmpeg");
        if (!Directory.Exists(DownloadFolder)) {
            Directory.CreateDirectory(DownloadFolder);
        }
        string DownloadPath = Path.Combine(DownloadFolder, "ffmpeg.zip");
        string InstallPath = Path.Combine("./VidCutter/", Path.Combine("ffmpeg", "ffmpeg"));
        try {
            Logger.Info("Vidcutter", $"Starting download of {DownloadURL}");
            progress.LogLine(Dialog.Clean("VIDCUTTER_DOWNLOADINGFFMPEG"));
            Everest.Updater.DownloadFileWithProgress(DownloadURL, DownloadPath, (position, length, speed) => {
                        if (length > 0) {
                            progress.Lines[progress.Lines.Count - 1] =
                                Dialog.Clean("VIDCUTTER_DOWNLOADINGFFMPEG") + $" {(int) Math.Floor(100D * (position / (double) length))}% @ {speed} KiB/s";
                            progress.Progress = position;
                        } else {
                            progress.Lines[progress.Lines.Count - 1] =
                                Dialog.Clean("VIDCUTTER_DOWNLOADINGFFMPEG") + $" {(int) Math.Floor(position / 1000D)}KiB @ {speed} KiB/s";
                        }

                        progress.ProgressMax = (int) length;
                        return true;
                    });
            if (!File.Exists(DownloadPath)) {
                Logger.Error("Vidcutter", $"Download failed! The ZIP file went missing");
                return false;
            }

            ZipFile.ExtractToDirectory(DownloadPath, InstallPath);

            if (File.Exists(DownloadPath))
                File.Delete(DownloadPath);

            return true;
        } catch (Exception ex) {
            Logger.Error("Vidcutter", ex.StackTrace+" "+ex.Message);
            return false;
        }
    }

    public override void Load() {
        string logFolder = Path.Combine("./VidCutter/", Path.Combine("logs"));
        if (!Directory.Exists(logFolder)) {
            Directory.CreateDirectory(logFolder);
        }
        LogManager.logPath = Path.Combine("./VidCutter/", Path.Combine("logs", "log.txt"));
        LogManager.LogFileWriter = new StreamWriter(LogManager.logPath, true) {
            AutoFlush = true
        };
        On.Celeste.Level.RegisterAreaComplete += OnComplete;
        On.Celeste.Level.Begin += OnBegin;
        On.Celeste.Level.LoadLevel += OnDeath;
        On.Celeste.Player.Update += onPlayerUpdate;
        On.Celeste.Strawberry.OnCollect += OnCollectStrawberry;
        On.Celeste.Cassette.CollectRoutine += OnCollectCassette;
        On.Celeste.LevelExit.ctor += OnRestart;
        typeof(VidcutterSpeedrunToolImport).ModInterop();
        SpeedrunToolInstalled = VidcutterSpeedrunToolImport.IgnoreSaveState is not null;
        if (SpeedrunToolInstalled) {
            action = VidcutterSpeedrunToolImport.RegisterSaveLoadAction(
                (_, level) => {},
                (_, level) => { onLoadState(level); },
                null,
                null,
                null,
                null
            );
        }
        DurationCache = new Dictionary<string, TimeSpan>();

        string cacheFile = Path.Combine("./VidCutter/", "durationCache.txt");
        if (File.Exists(cacheFile)) {
            string[] lines = File.ReadAllLines(cacheFile);
            foreach (string line in lines) {
                string[] splitted = line.Split(" | ");
                if (splitted.Length == 2) {
                    DurationCache[splitted[0]] = TimeSpan.Parse(splitted[1]);
                }
            }
        }
    }

    public static void writeCache(string video, TimeSpan duration) {
        if (DurationCache != null && !DurationCache.ContainsKey(video)) {
            DurationCache[video] = duration;
        }
        string cacheFile = Path.Combine("./VidCutter/", "durationCache.txt");
        using (StreamWriter writer = new StreamWriter(cacheFile, false)) {
            foreach (KeyValuePair<string, TimeSpan> entry in DurationCache) {
                writer.WriteLine($"{entry.Key} | {entry.Value}");
            }
        }
    }

    public override void Unload() {
        LogManager.LogFileWriter.Close();
        On.Celeste.Level.RegisterAreaComplete -= OnComplete;
        On.Celeste.Level.Begin -= OnBegin;
        On.Celeste.Level.LoadLevel -= OnDeath;
        On.Celeste.Player.Update -= onPlayerUpdate;
        On.Celeste.Strawberry.OnCollect -= OnCollectStrawberry;
        On.Celeste.Cassette.CollectRoutine -= OnCollectCassette;
        On.Celeste.LevelExit.ctor -= OnRestart;
        if (SpeedrunToolInstalled) {
            VidcutterSpeedrunToolImport.Unregister(action);
        }
    }
}