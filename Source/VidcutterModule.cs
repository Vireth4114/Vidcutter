using System;
using Celeste.Mod.Vidcutter.Hooks;

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
        HookManager.LoadAll();
    }

    public override void Unload() {
        HookManager.UnloadAll();
    }
}
