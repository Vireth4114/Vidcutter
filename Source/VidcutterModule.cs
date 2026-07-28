using System;
using Celeste.Mod.Vidcutter.Hooks;
using Celeste.Mod.Vidcutter.Utils;

namespace Celeste.Mod.Vidcutter;

public class VidcutterModule : EverestModule {
    private static VidcutterModule Instance { get; set; }

    public override Type SettingsType => typeof(VidcutterModuleSettings);
    public static VidcutterModuleSettings Settings => (VidcutterModuleSettings)Instance._Settings;

    public static readonly VidcutterState State = new();

    public VidcutterModule() {
        Instance = this;
        Logger.SetLogLevel(nameof(VidcutterModule), LogLevel.Info);
    }

    public override void Load() {
        LogManager.Initialize();
        HookManager.LoadAll();
        VideoFile.LoadDurationCache();
    }

    public override void Unload() {
        LogManager.CloseWriter();
        HookManager.UnloadAll();
    }
}