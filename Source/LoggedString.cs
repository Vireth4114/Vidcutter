using System;

namespace Celeste.Mod.Vidcutter;

public class LoggedString {
    public DateTime Time;
    public string Event;
    public string Level;
    public string Room;
    public LoggedString(DateTime time, string _event, string level, string room){
        Time = time;
        Event = _event;
        Level = level;
        Room = room;
    }

    public bool isCleared() {
        string[] clearedEvents = {"ROOM PASSED", "LEVEL COMPLETE"};
        return clearedEvents.Contains(Event);
    }

    public override string ToString() {
        return $"[{Time:yyyy-MM-dd HH:mm:ss.fff}] {Level} | {Room} | {Event}";
    }
}