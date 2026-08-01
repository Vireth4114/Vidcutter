using System.Collections;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Vidcutter.Entities;

public class Tooltip : Entity {
    private const int Padding = 25;
    private readonly string _message;
    protected float Alpha;
    private float _unEasedAlpha;
    private readonly float _duration;

    protected Tooltip(string message, float duration = 1f) {
        Logger.Info("Vidcutter", "Showing tooltip: " + message);
        _message = message;
        _duration = duration;
        Vector2 messageSize = ActiveFont.Measure(message);
        Position = new Vector2(Padding, Engine.Height - messageSize.Y - Padding / 2f);
        Tag = Tags.HUD | Tags.Global | Tags.FrozenUpdate | Tags.PauseUpdate | Tags.TransitionUpdate;
        Add(new Coroutine(Show()));
    }

    private IEnumerator Show() {
        while (Alpha < 1f) {
            _unEasedAlpha = Calc.Approach(_unEasedAlpha, 1f, Engine.RawDeltaTime * 5f);
            Alpha = Ease.SineOut(_unEasedAlpha);
            yield return null;
        }

        yield return Dismiss();
    }

    protected virtual IEnumerator Dismiss() {
        yield return _duration;
        while (Alpha > 0f) {
            _unEasedAlpha = Calc.Approach(_unEasedAlpha, 0f, Engine.RawDeltaTime * 5f);
            Alpha = Ease.SineIn(_unEasedAlpha);
            yield return null;
        }

        RemoveSelf();
    }

    public override void Render() {
        base.Render();
        ActiveFont.DrawOutline(
            _message,
            position: Position,
            justify: Vector2.Zero,
            scale: Vector2.One,
            color: Color.White * Alpha,
            stroke: 2,
            strokeColor: Color.Black * Alpha * Alpha * Alpha
        );
    }

    public static void Show(string message, float duration = 1f) {
        if (Engine.Scene is not { } scene) return;
        scene.Entities
            .FindAll<Tooltip>()
            .Where(tooltip => tooltip is not TooltipWithProgress)
            .ToList()
            .ForEach(entity => entity.RemoveSelf());
        scene.Add(new Tooltip(message, duration));
    }
}
