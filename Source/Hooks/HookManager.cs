namespace Celeste.Mod.Vidcutter.Hooks;

public static class HookManager {
    public static void LoadAll() {
        VanillaHooks.Load();
        VivHelperHooks.Load();
        SpeedrunToolHooks.Load();
        LogFileHooks.Load();
    }

    public static void UnloadAll() {
        VanillaHooks.Unload();
        VivHelperHooks.Unload();
        SpeedrunToolHooks.Unload();
        LogFileHooks.Unload();
    } 
}