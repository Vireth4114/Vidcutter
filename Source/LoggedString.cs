using System;

namespace Celeste.Mod.Vidcutter;

public class LoggedString {
    public DateTime Time;
    public string Event;
    public string Level;
    public string Room;
    public bool CountTowardsClear;
    public LoggedString(DateTime time, string _event, string level, string room, string countTowardsClear) {
        Time = time;
        Event = _event;
        Level = level;
        Room = room;
        CountTowardsClear = countTowardsClear == null || bool.Parse(countTowardsClear);
    }

    public bool isCleared() {
        string[] clearedEvents = {"ROOM PASSED", "LEVEL COMPLETE"};
        return clearedEvents.Contains(Event);
    }

    public override string ToString() {
        return $"[{Time:yyyy-MM-dd HH:mm:ss.fff}] {Level} | {Room} | {Event}";
    }
}