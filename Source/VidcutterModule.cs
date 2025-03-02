using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Vidcutter;

public class VidcutterModule : EverestModule {
    public static VidcutterModule Instance { get; private set; }

    public override Type SettingsType => typeof(VidcutterModuleSettings);
    public static VidcutterModuleSettings Settings => (VidcutterModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(VidcutterModuleSession);
    public static VidcutterModuleSession Session => (VidcutterModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(VidcutterModuleSaveData);
    public static VidcutterModuleSaveData SaveData => (VidcutterModuleSaveData) Instance._SaveData;

    public static string logPath;
    public static StreamWriter LogFileWriter = null;
    public static string logPath2;
    public static StreamWriter LogFileWriter2 = null;

    public VidcutterModule() {
        Instance = this;
        Logger.SetLogLevel(nameof(VidcutterModule), LogLevel.Info);
    }

    public static void Log(string message, bool debug = false, Session session = null) {
        string toLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ";
        if (session != null) {
            string sid = session.Area.SID;
            if (sid.StartsWith("Celeste/")) {
                sid = $"AREA_{sid.Substring(8, 1)}";
            }
            toLog += Dialog.Clean(sid);
            if (session.Area.Mode.ToString().EndsWith("Side")) {
                toLog += $" [{session.Area.Mode.ToString()[0]}-Side]";
            }
            toLog += $" | {session.Level} | ";
        }
        toLog += message;
        if (debug) {
            LogFileWriter2.WriteLine(toLog);
        } else {
            LogFileWriter.WriteLine(toLog);
        }
    }

    public static List<LoggedString> getAllLogs(string video, string level = null) {
        DateTime startVideo = File.GetCreationTime(video);
        TimeSpan? duration = VideoCreation.getVideoDuration(video);
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
        Log("LEVEL COMPLETE", session: self.Session);
        orig(self);
    }

    public static void OnTransition(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader = false) {
        if (playerIntro == Player.IntroTypes.Transition) {
            Log("ROOM PASSED", session: self.Session);
        } else if (playerIntro == Player.IntroTypes.Respawn) {
            Log("DEATH", session: self.Session);
        }
        orig(self, playerIntro, isFromLoader);
    }

    public static void OnBegin(On.Celeste.Level.orig_Begin orig, Level self) {
        Log("LEVEL LOADED", session: self.Session);
        orig(self);
    }

    public static void OnScreenWipe(On.Celeste.Level.orig_DoScreenWipe orig, Level self, bool wipeIn, Action onComplete = null, bool hiresSnow = false) {
        if (onComplete != null && wipeIn) {
            Log("STATE", session: self.Session);
        }
        orig(self, wipeIn, onComplete, hiresSnow);
    }

    public static void OnEnter(On.Celeste.ChangeRespawnTrigger.orig_OnEnter orig, ChangeRespawnTrigger self, Player player) {
        Log("ROOM PASSED", session: player.SceneAs<Level>().Session);
        orig(self, player);
    }
    
    public static void OnFlag(On.Monocle.Entity.orig_Added orig, Entity self, Scene scene) {
        orig(self, scene);
        if (self is SummitCheckpoint.ConfettiRenderer confetti) {
            Log("ROOM PASSED", session: confetti.SceneAs<Level>().Session);
        }
    }

    public static void OnCollectStrawberry(On.Celeste.Strawberry.orig_OnCollect orig, Strawberry self) {
        Log("ROOM PASSED", session: self.SceneAs<Level>().Session);
        orig(self);
    }

    public static IEnumerator OnCollectHeart(On.Celeste.HeartGem.orig_CollectRoutine orig, HeartGem self, Player player) {
        yield return new SwapImmediately(orig(self, player));
        Log("ROOM PASSED", session: self.SceneAs<Level>().Session);
    }

    public static IEnumerator OnCollectCassette(On.Celeste.Cassette.orig_CollectRoutine orig, Cassette self, Player player) {
        yield return new SwapImmediately(orig(self, player));
        Log("ROOM PASSED", session: self.SceneAs<Level>().Session);
    }

    public static bool InstallFFmpeg(OuiLoggedProgress progress) {
        // Code mostly taken from psyGamer's TASRecorder because ffmpeg is a mess to work with
        string DownloadURL = "https://www.gyan.dev/ffmpeg/builds/packages/ffmpeg-7.1-essentials_build.zip";
        string DownloadFolder = Path.Combine("./VidCutter/", "ffmpeg");
        if (!Directory.Exists(DownloadFolder)) {
            Directory.CreateDirectory(DownloadFolder);
        }
        string DownloadPath = Path.Combine(DownloadFolder, "ffmpeg.zip");
        string InstallPath = Path.Combine("./VidCutter/", Path.Combine("ffmpeg", "ffmpeg"));
        try {
            Logger.Info("Vidcutter", $"Starting download of {DownloadURL}");
            progress.LogLine("Downloading FFmpeg");
            Everest.Updater.DownloadFileWithProgress(DownloadURL, DownloadPath, (position, length, speed) => {
                        if (length > 0) {
                            progress.Lines[progress.Lines.Count - 1] =
                                $"Downloading FFmpeg {(int) Math.Floor(100D * (position / (double) length))}% @ {speed} KiB/s";
                            progress.Progress = position;
                        } else {
                            progress.Lines[progress.Lines.Count - 1] =
                                $"Downloading FFmpeg {(int) Math.Floor(position / 1000D)}KiB @ {speed} KiB/s";
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
        logPath2 = Path.Combine("./VidCutter/", Path.Combine("logs", "log2.txt"));
        LogFileWriter = new StreamWriter(logPath, true) {
            AutoFlush = true
        };
        LogFileWriter2 = new StreamWriter(logPath2, true) {
            AutoFlush = true
        };
        On.Celeste.Level.RegisterAreaComplete += OnComplete;
        On.Celeste.Level.Begin += OnBegin;
        On.Celeste.Level.LoadLevel += OnTransition;
        On.Celeste.Level.DoScreenWipe += OnScreenWipe;
        On.Celeste.ChangeRespawnTrigger.OnEnter += OnEnter;
        On.Monocle.Entity.Added += OnFlag;
        On.Celeste.Strawberry.OnCollect += OnCollectStrawberry;
        On.Celeste.HeartGem.CollectRoutine += OnCollectHeart;
        On.Celeste.Cassette.CollectRoutine += OnCollectCassette;
    }

    public override void Unload() {
        LogFileWriter.Close();
        LogFileWriter2.Close();
        On.Celeste.Level.RegisterAreaComplete -= OnComplete;
        On.Celeste.Level.Begin -= OnBegin;
        On.Celeste.Level.LoadLevel -= OnTransition;
        On.Celeste.Level.DoScreenWipe -= OnScreenWipe;
        On.Celeste.ChangeRespawnTrigger.OnEnter -= OnEnter;
        On.Monocle.Entity.Added -= OnFlag;
        On.Celeste.Strawberry.OnCollect -= OnCollectStrawberry;
        On.Celeste.HeartGem.CollectRoutine -= OnCollectHeart;
        On.Celeste.Cassette.CollectRoutine -= OnCollectCassette;
    }
}