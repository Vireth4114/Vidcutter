using System.Collections;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Vidcutter;

public class Tooltip : Entity {
    protected const int Padding = 25;
    protected readonly string message;
    protected float alpha;
    protected float unEasedAlpha;
    protected readonly float duration;

    public Tooltip(string message, float duration = 1f) {
        Logger.Info("Vidcutter", "Showing tooltip: " + message);
        this.message = message;
        this.duration = duration;
        Vector2 messageSize = ActiveFont.Measure(message);
        Position = new(Padding, Engine.Height - messageSize.Y - Padding / 2f);
        Tag = Tags.HUD | Tags.Global | Tags.FrozenUpdate | Tags.PauseUpdate | Tags.TransitionUpdate;
        Add(new Coroutine(Show()));
    }

    protected IEnumerator Show() {
        while (alpha < 1f) {
            unEasedAlpha = Calc.Approach(unEasedAlpha, 1f, Engine.RawDeltaTime * 5f);
            alpha = Ease.SineOut(unEasedAlpha);
            yield return null;
        }

        yield return Dismiss();
    }

    protected virtual IEnumerator Dismiss() {
        yield return duration;
        while (alpha > 0f) {
            unEasedAlpha = Calc.Approach(unEasedAlpha, 0f, Engine.RawDeltaTime * 5f);
            alpha = Ease.SineIn(unEasedAlpha);
            yield return null;
        }

        RemoveSelf();
    }

    public override void Render() {
        base.Render();
        ActiveFont.DrawOutline(message, Position, Vector2.Zero, Vector2.One, Color.White * alpha, 2,
            Color.Black * alpha * alpha * alpha);
    }

    public static void Show(string message, float duration = 1f) {
        if (Engine.Scene is { } scene) {
            if (!scene.Tracker.Entities.TryGetValue(typeof(Tooltip), out var tooltips)) {
                tooltips = [..scene.Entities.FindAll<Tooltip>()
                    .Where(tooltip => tooltip is not TooltipWithProgress)
                    .ToList().Cast<Entity>()];
            }
            tooltips.ForEach(entity => entity.RemoveSelf());
            scene.Add(new Tooltip(message, duration));
        }
    }
}
