using System.Collections.Generic;
using Celeste.Mod.Vidcutter.Models;
using Celeste.Mod.Vidcutter.Services.Logs;

namespace Celeste.Mod.Vidcutter.Hooks;

public static class HookManager{
    public static HookState State { get; } = new();
    public static ISettings Settings { get; private set; }
    
    private static readonly List<IHook> Hooks = [
        new VanillaLoggingHooks(),
        new VivHelperHooks(),
        new SpeedrunToolHooks(),
        new HotkeyHooks(),
        new LevelLifeCycleHooks()
    ];

    public static void LoadAll(ISettings settings) {
        Settings = settings;
        foreach (IHook hook in Hooks) {
            hook.Load();
        }
    }

    public static void UnloadAll() {
        foreach (IHook hook in Hooks) {
            hook.Unload();
        }
        Settings = null;
    }

    public static void Log(string message, Session session) {
        LoggedString log = LoggedString.GetFromSession(message, session);
        
        if (!State.IsFromASavestate && log.IsCleared()) {
            if (State.LastEvent != null && !State.LastEvent.IsCleared()) {
                LogService.WriteLog(State.LastEvent);
            } 
            LogService.WriteLog(log);
        }

        State.LastEvent = log;
    }
}