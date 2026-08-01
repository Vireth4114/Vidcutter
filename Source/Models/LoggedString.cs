using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.Vidcutter.Models;

public record LoggedString(
    DateTime Time,
    string Event,
    string Level,
    string Room,
    bool? CountTowardsClear = null
) {
    private static readonly HashSet<string> ClearedEvents = [
        "ROOM PASSED", "LEVEL COMPLETE", "CLOSE TO SPAWNPOINT", "DEATH AFTER COLLECTIBLE"
    ];
    
    private static readonly HashSet<string> CollectableEvents = [
        "BERRY", "CASSETTE", "HEART", "KEY", "SUMMIT_GEM"
    ];

    public static LoggedString Parse(string line) {
        DateTime logTime = DateTime.Parse(line[1..24]);
        string[] loggedEvent = line[26..].Split(" | ");
        bool? countTowardsClear = loggedEvent.ElementAtOrDefault(3) != null ? bool.Parse(loggedEvent[3]) : null;
        return new LoggedString(logTime, loggedEvent[2], loggedEvent[0], loggedEvent[1], countTowardsClear);
    }
    
    public bool IsCleared() {
        return ClearedEvents.Contains(Event) || CollectableEvents.Contains(Event) || BackToStartOfInterRoom();
    }
    
    public bool IsCollectable() {
        return CollectableEvents.Contains(Event);
    }
    
    public bool BackToStartOfInterRoom() {
        return Event is "BACK TO START OF INTER ROOM" or "INTER ROOM PASSED";
    }

    public override string ToString() {
        return $"[{Time:yyyy-MM-dd HH:mm:ss.fff}] {Level} | {Room} | {Event}" + (CountTowardsClear == null ? "" : $" | {CountTowardsClear}");
    }
}