using System.Collections.Generic;
using Celeste.Mod.Vidcutter.Models;

namespace Celeste.Mod.Vidcutter.Hooks;

public class HookManager(VidcutterModuleSettings settings, LoggingState state) {
    private readonly List<IHook> _hooks = [
        new VanillaLoggingHooks(state),
        new VivHelperHooks(state),
        new SpeedrunToolHooks(state),
        new HotkeyHooks(settings, state),
        new LogFileHooks()
    ];

    public void LoadAll() {
        foreach (IHook hook in _hooks) {
            hook.Load();
        }
    }

    public void UnloadAll() {
        foreach (IHook hook in _hooks) {
            hook.Unload();
        }
    } 
}