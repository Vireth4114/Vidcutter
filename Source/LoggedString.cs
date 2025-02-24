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
}