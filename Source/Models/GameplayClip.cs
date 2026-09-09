using System;

namespace Celeste.Mod.Vidcutter.Models;

public record GameplayClip(LoggedString Start, LoggedString End, float StartDelay = 0, float EndDelay = 0) {
    public DateTime StartTimeWithDelay => Start.Time + TimeSpan.FromSeconds(StartDelay);

    public DateTime EndTimeWithDelay => End.Time + TimeSpan.FromSeconds(EndDelay);

    public double Duration => (EndTimeWithDelay - StartTimeWithDelay).TotalSeconds;

    public string Level => Start.Level;

    public override string ToString() {
        return $"Start: {Start}, End: {End}";
    }
}