using System.Collections;
using System.Linq;
using System.Reflection.PortableExecutable;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Vidcutter;

public class TooltipWithProgress(string message) : Tooltip(message, 0) {
    public float progress = 0f;

    protected override IEnumerator Dismiss() {
        while (progress < 1f) {
            yield return null;
        }
        yield return base.Dismiss();
    }

    public override void Render() {
        base.Render();
        Draw.Line(new Vector2(0, Engine.Height), new Vector2(Engine.Width * progress, Engine.Height), Color.White * alpha, 10f);
    }

    public static TooltipWithProgress Show(string message) {
        if (Engine.Scene is { } scene) {
            if (!scene.Tracker.Entities.TryGetValue(typeof(Tooltip), out var tooltips)) {
                tooltips = [..scene.Entities.FindAll<Tooltip>()
                    .ToList().Cast<Entity>()];
            }
            tooltips.ForEach(entity => entity.RemoveSelf());
            TooltipWithProgress progress = new(message);
            scene.Add(progress);
            return progress;
        }
        return null;
    }
}