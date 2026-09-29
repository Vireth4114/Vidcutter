using Celeste.Mod.Vidcutter.Services.Logs;

namespace Celeste.Mod.Vidcutter.Hooks;

public class LogFileHooks : IHook {
    private static void OnLevelBegin(On.Celeste.Level.orig_Begin orig, Level self) {
        LogService.SwitchToWriter();
        orig(self);
    }
    
    private static void OnLevelEnd(On.Celeste.Level.orig_End orig, Level self) {
        orig(self);
        LogService.SwitchToReader();
    }
    
    public void Load() {
        On.Celeste.Level.Begin += OnLevelBegin;
        On.Celeste.Level.End += OnLevelEnd;
    }

    public void Unload() {
        On.Celeste.Level.Begin -= OnLevelBegin;
        On.Celeste.Level.End -= OnLevelEnd;
    }
}


