using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Threading;
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

    public static string logPath;
    public static StreamWriter LogFileWriter = null;

    public static Vector2? previousRespawnPoint = null;
    public static Level previousLevel = null;
    public static bool processWhenClose = false;
    private static bool SpeedrunToolInstalled = false;
    private static bool inState = false;
    private static object action;

    public VidcutterModule() {
        Instance = this;
        Logger.SetLogLevel(nameof(VidcutterModule), LogLevel.Info);
    }

    public static void Log(string message, Session session = null) {
        string toLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ";
        if (session != null) {
            string sid = session.Area.SID;
            if (sid.StartsWith("Celeste/")) {
                sid = $"AREA_{sid.Substring(8, 1)}";
                if (sid == "AREA_L") {
                    sid = "AREA_10";
                }
            }
            toLog += Dialog.Clean(sid);
            if (session.Area.Mode.ToString().EndsWith("Side")) {
                toLog += $" [{session.Area.Mode.ToString()[0]}-Side]";
            }
            toLog += $" | {session.Level} | ";
        }
        toLog += message;
        LogFileWriter.WriteLine(toLog);
    }

    public static List<LoggedString> getAllLogs(string video, string level = null) {
        DateTime startVideo = File.GetCreationTime(video);
        TimeSpan? duration = VideoCreation.getVideoDuration(video);
        Logger.Info("Vidcutter", $"Video {video} started at {startVideo} and has duration {duration}");
        if (duration == null) {
            return new List<LoggedString>();
        }
        DateTime endVideo = startVideo + (TimeSpan)duration;
        return getAllLogs(startVideo, endVideo, level);
    }

    public static List<LoggedString> getAllLogs(DateTime? startVideo = null, DateTime? endVideo = null, string level = null) {
        LogFileWriter.Close();
        string[] lines = File.ReadAllLines(logPath);
        List<LoggedString> parsedLines = new List<LoggedString>();
        foreach (string line in lines) {
            DateTime logTime = DateTime.Parse(line.Substring(1, 23));
            string[] loggedEvent = line.Substring(26).Split(" | ");
            bool condition = true;
            if (startVideo != null) {
                condition &= startVideo <= logTime;
            }
            if (endVideo != null) {
                condition &= logTime <= endVideo;
            }
            if (level != null) {
                condition &= loggedEvent[0] == level;
            }
            if (condition) {
                parsedLines.Add(new LoggedString(logTime, loggedEvent[2], loggedEvent[0], loggedEvent[1]));
            }
        }

        LogFileWriter = new StreamWriter(logPath, true) {
            AutoFlush = true
        };
        return parsedLines;
    }

    public static void OnComplete(On.Celeste.Level.orig_RegisterAreaComplete orig, Level self) {
        if (!self.Completed) {
            Log("LEVEL COMPLETE", session: self.Session);
            processWhenClose = false;
        }
        orig(self);
    }

    public static void OnDeath(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader = false) {
        if (playerIntro == Player.IntroTypes.Respawn) {
            Log("DEATH", session: self.Session);
            processWhenClose = false;
            inState = false;
        }
        orig(self, playerIntro, isFromLoader);
    }

    public static void OnBegin(On.Celeste.Level.orig_Begin orig, Level self) {
        Log("LEVEL LOADED", session: self.Session);
        inState = false;
        orig(self);
    }

    public static void OnCollectStrawberry(On.Celeste.Strawberry.orig_OnCollect orig, Strawberry self) {
        if (!inState) {
            Log("ROOM PASSED", session: self.SceneAs<Level>().Session);
        }
        orig(self);
    }

    public static IEnumerator OnCollectCassette(On.Celeste.Cassette.orig_CollectRoutine orig, Cassette self, Player player) {
        yield return new SwapImmediately(orig(self, player));
        if (!inState) {
            Log("ROOM PASSED", session: self.SceneAs<Level>().Session);
        }
    }

    public static void OnRestart(On.Celeste.LevelExit.orig_ctor orig, LevelExit self, LevelExit.Mode mode, Session session, HiresSnow snow) {
        if (mode == LevelExit.Mode.Restart) {
            Log("RESTART CHAPTER", session: session);
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
            Log($"ROOM PASSED", session: self.SceneAs<Level>().Session);
            processWhenClose = true;
        }
        float deltaY = Math.Abs(playerPos.Y - respawnPoint.Value.Y);
        float deltaX = Math.Abs(playerPos.X - respawnPoint.Value.X);
        double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        if (distance <= 50 && processWhenClose && !inState) {
            Log($"ROOM PASSED", session: self.SceneAs<Level>().Session);
            processWhenClose = false;
        }
    }

    public static void onLoadState(Level level)
    {
        Log("STATE", session: level.Session);
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
        logPath = Path.Combine("./VidCutter/", Path.Combine("logs", "log.txt"));
        LogFileWriter = new StreamWriter(logPath, true) {
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
    }

    public override void Unload() {
        LogFileWriter.Close();
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