using System;
using System.Collections.Generic;

namespace Celeste.Mod.Vidcutter.Models;

public record LoggedString(
    DateTime Time,
    string Event,
    string Level,
    string Room,
    bool CountTowardsClear
) {
    private static readonly HashSet<string> ClearedEvents = [
        "ROOM PASSED", "LEVEL COMPLETE", "CLOSE TO SPAWNPOINT"
    ];
    
    private static readonly HashSet<string> CollectableEvents = [
        "BERRY", "CASSETTE", "HEART", "KEY", "SUMMIT_GEM"
    ];
    
    public bool IsCleared() {
        return ClearedEvents.Contains(Event) || CollectableEvents.Contains(Event);
    }
    
    public bool IsCollectable() {
        return CollectableEvents.Contains(Event);
    }
    
    public bool BackToStartOfInterRoom() {
        return Event is "BACK TO START OF INTER ROOM" or "INTER ROOM PASSED";
    }

    public override string ToString() {
        return $"[{Time:yyyy-MM-dd HH:mm:ss.fff}] {Level} | {Room} | {Event} | {CountTowardsClear}";
    }
}