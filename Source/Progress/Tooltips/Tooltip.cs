using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Vidcutter.Progress.Tooltips;

public abstract class Tooltip : Entity {
    private const int Padding = 25;
    public string Message { get; set; }
    protected float Alpha;
    private float _unEasedAlpha;

    protected Tooltip() {
        Vector2 messageSize = ActiveFont.Measure(' ');
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
            Message,
            position: Position,
            justify: Vector2.Zero,
            scale: Vector2.One,
            color: Color.White * Alpha,
            stroke: 2,
            strokeColor: Color.Black * Alpha * Alpha * Alpha
        );
    }
}
