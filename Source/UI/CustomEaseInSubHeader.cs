using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Vidcutter.UI;


public class CustomEaseInSubHeader : TextMenuExt.SubHeaderExt {
    private float _uneasedAlpha;
    public bool FadeVisible { get; set; }

    public CustomEaseInSubHeader(string title) : base(title) {
        Alpha = 0f;
        _uneasedAlpha = Alpha;
        TextColor = Color.Gray;
        HeightExtra = 0f;
    }

    public override float Height() {
        return MathHelper.Lerp(-4f, base.Height(), Alpha);
    }

    public override void Update() {
        base.Update();
        float target = FadeVisible ? 1 : 0;
        if (Math.Abs(_uneasedAlpha - target) > 0.001f) {
            _uneasedAlpha = Calc.Approach(_uneasedAlpha, target, Engine.RawDeltaTime * 3f);
            Alpha = FadeVisible ? Ease.SineOut(_uneasedAlpha) : Ease.SineIn(_uneasedAlpha);
        }
        Visible = Alpha != 0.0;
    }
}