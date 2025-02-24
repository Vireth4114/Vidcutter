using System;
using System.IO;

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

    public static void Log(string message, bool debug = false, Level level = null) {
        string toLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ";
        if (level != null) {
            string sid = level.Session.Area.GetSID();
            string[] splitted = sid.Split('/');
            toLog += $"{splitted[splitted.Length - 1]} | {level.Session.Level} | ";
        }
        toLog += message;
        if (debug) {
            LogFileWriter2.WriteLine(toLog);
        } else {
            LogFileWriter.WriteLine(toLog);
        }
    }

    private void OnComplete(On.Celeste.Level.orig_RegisterAreaComplete orig, Level self) {
        Log("LEVEL COMPLETE", level: self);
        orig(self);
    }

    private void OnTransition(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader = false) {
        if (playerIntro == Player.IntroTypes.Transition) {
            Log("ROOM PASSED", level: self);
        } else if (playerIntro == Player.IntroTypes.Respawn) {
            Log("DEATH", level: self);
        }
        orig(self, playerIntro, isFromLoader);
    }

    private void OnBegin(On.Celeste.Level.orig_Begin orig, Level self) {
        Log("LEVEL LOADED", level: self);
        orig(self);
    }

    private void OnScreenWipe(On.Celeste.Level.orig_DoScreenWipe orig, Level self, bool wipeIn, Action onComplete = null, bool hiresSnow = false) {
        if (onComplete != null && wipeIn) {
            Log("STATE", level: self);
        }
        orig(self, wipeIn, onComplete, hiresSnow);
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
    }

    public override void Unload() {
        LogFileWriter.Close();
        LogFileWriter2.Close();
        On.Celeste.Level.RegisterAreaComplete -= OnComplete;
        On.Celeste.Level.Begin -= OnBegin;
        On.Celeste.Level.LoadLevel -= OnTransition;
        On.Celeste.Level.DoScreenWipe -= OnScreenWipe;
    }
}