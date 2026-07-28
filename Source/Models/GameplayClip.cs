using System;

namespace Celeste.Mod.Vidcutter.Models;

public record GameplayClip(LoggedString Start, LoggedString End)
{
    private static VidcutterModuleSettings Settings => VidcutterModule.Settings;
    
    public DateTime StartTimeWithDelay => Start.Time + TimeSpan.FromSeconds(Settings.DelayStart);
    
    public DateTime EndTimeWithDelay => End.Time + TimeSpan.FromSeconds(
        End.Event == "LEVEL COMPLETE" ? Settings.DelayComplete : Settings.DelayEnd
    );

    public double Duration => (EndTimeWithDelay - StartTimeWithDelay).TotalSeconds;

    public string Level => Start.Level;

    public override string ToString() {
        return $"Start: {Start}, End: {End}";
    }
}