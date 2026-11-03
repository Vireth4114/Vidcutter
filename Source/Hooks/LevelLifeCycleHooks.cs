using Celeste.Mod.Vidcutter.Services.Logs;

namespace Celeste.Mod.Vidcutter.Hooks;

public class LevelLifeCycleHooks : IHook {
    private static HookState State => HookManager.State;

    private static void OnLevelBegin(On.Celeste.Level.orig_Begin orig, Level self) {
        State.IsFromASavestate = false;
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


