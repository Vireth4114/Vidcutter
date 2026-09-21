namespace Celeste.Mod.Vidcutter.Models;

public record ClipDelays(
    float Start,
    float End,
    int Complete
) {
    public static ClipDelays FromSettings(VidcutterModuleSettings settings) => new(
        settings.DelayStart,
        settings.DelayEnd,
        settings.DelayComplete
    );
}