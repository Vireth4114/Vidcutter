using System;
using Celeste.Mod.Vidcutter.Hooks;
using Celeste.Mod.Vidcutter.Services.Logs;

namespace Celeste.Mod.Vidcutter;

public class VidcutterModule : EverestModule {
    public override Type SettingsType => typeof(VidcutterModuleSettings);
    private VidcutterModuleSettings Settings => (VidcutterModuleSettings)_Settings;

    public VidcutterModule() {
        Logger.SetLogLevel(nameof(VidcutterModule), LogLevel.Info);
    }

    public override void Initialize() {
        LogService.SwitchToReader();
    }

    public override void Load() {
        HookManager.LoadAll(Settings);
    }

    public override void Unload() {
        HookManager.UnloadAll();
        LogService.Close();
    }
}
