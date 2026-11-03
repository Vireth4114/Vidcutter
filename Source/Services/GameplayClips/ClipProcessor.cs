using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Vidcutter.Models;

namespace Celeste.Mod.Vidcutter.Services.GameplayClips;

public class ClipProcessor(ClipDelays delays) {
    private readonly GameplayClipFactory _clipFactory = new(delays);

    public ClipProcessor() : this(new ClipDelays(0, 0, 0)) { }

    public List<GameplayClip> GetClipsFromLogs(List<LoggedString> parsedLines) {
        List<GameplayClip> processedClips = [];
        List<LoggedString> logsForCurrentClip = [];
        for (int i = 0; i < parsedLines.Count; i++) {
            LoggedString currentLine = parsedLines[i];
            LoggedString nextLine = i < parsedLines.Count - 1 ? parsedLines[i + 1] : null;

            if (currentLine.Event == "RESTART CHAPTER") {
                processedClips = processedClips.Where(clip => clip.Level != currentLine.Level).ToList();
                logsForCurrentClip.Clear();
                continue;
            }

            if (nextLine != null && nextLine.Level == currentLine.Level && nextLine.IsCleared() && nextLine.CountTowardsClear != false) {
                logsForCurrentClip.Add(currentLine);
                continue;
            }

            if (logsForCurrentClip.Count == 0)
                continue;
            
            LoggedString clipEnd = currentLine;

            if (clipEnd.BackToStartOfInterRoom()) {
                clipEnd = LastClearedLogToRoom(logsForCurrentClip, clipEnd.Room);
                if (clipEnd == null) {
                    logsForCurrentClip.Clear();
                    continue;
                }
            }
        
            processedClips.Add(_clipFactory.Create(logsForCurrentClip[0], clipEnd));
            logsForCurrentClip.Clear();
        }
        return processedClips;
    }

    private LoggedString LastClearedLogToRoom(List<LoggedString> logs, string room) {
        return logs.LastOrDefault(log => log.Room == room && log.IsCleared() && !log.BackToStartOfInterRoom());
    }
}