namespace Celeste.Mod.Vidcutter.Hooks;

public static class HookManager {
    public static void LoadAll() {
        VanillaLoggingHooks.Load();
        VivHelperHooks.Load();
        SpeedrunToolHooks.Load();
        LogFileHooks.Load();
        HotkeyHooks.Load();
    }

    public static void UnloadAll() {
        VanillaLoggingHooks.Unload();
        VivHelperHooks.Unload();
        SpeedrunToolHooks.Unload();
        LogFileHooks.Unload();
        HotkeyHooks.Unload();
    } 
}