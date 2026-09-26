using System.Collections;
using System.Linq;
using Monocle;

namespace Celeste.Mod.Vidcutter.Entities;

public class SimpleTooltip : Tooltip {
    private readonly float _duration;
    
    private SimpleTooltip(string message, float duration) {
        Logger.Info("Vidcutter", "Showing tooltip: " + message);
        _duration = duration;
        Message = message;
    }

    protected override IEnumerator Dismiss() {
        yield return _duration;
        yield return base.Dismiss();
    }

    public static void Show(string message, float duration = 1f) {
        if (Engine.Scene is not { } scene) return;
        scene.Entities
            .FindAll<Tooltip>()
            .ToList()
            .ForEach(entity => entity.RemoveSelf());
        scene.Add(new SimpleTooltip(message, duration));
    }
}