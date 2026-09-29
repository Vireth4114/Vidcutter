using System;
using Celeste.Mod.Vidcutter.Hooks;
using Celeste.Mod.Vidcutter.Models;
using Celeste.Mod.Vidcutter.Services.Logs;

namespace Celeste.Mod.Vidcutter;

public class VidcutterModule : EverestModule {
    private static VidcutterModule Instance { get; set; }
    private readonly HookManager _hookManager = new(Settings, new LoggingState());

    public override Type SettingsType => typeof(VidcutterModuleSettings);
    private static VidcutterModuleSettings Settings => (VidcutterModuleSettings)Instance._Settings;

    public VidcutterModule() {
        Instance = this;
        Logger.SetLogLevel(nameof(VidcutterModule), LogLevel.Info);
    }

    public override void Initialize() {
        LogService.SwitchToReader();
    }

    public override void Load() {
        _hookManager.LoadAll();
    }

    public override void Unload() {
        _hookManager.UnloadAll();
        LogService.Close();
    }
}
