using Celeste.Mod.Vidcutter.Utils;

namespace Celeste.Mod.Vidcutter.Hooks;

public class LogFileHooks {
    private static void OnLevelBegin(On.Celeste.Level.orig_Begin orig, Level self) {
        LogManager.OpenWriter();
        orig(self);
    }
    
    private static void OnLevelEnd(On.Celeste.Level.orig_End orig, Level self) {
        orig(self);
        LogManager.CloseWriter();
    }
    
    public static void Load() {
        On.Celeste.Level.Begin += OnLevelBegin;
        On.Celeste.Level.End += OnLevelEnd;
    }

    public static void Unload() {
        On.Celeste.Level.Begin -= OnLevelBegin;
        On.Celeste.Level.End -= OnLevelEnd;
    }
}


