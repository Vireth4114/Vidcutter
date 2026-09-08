using System;
using Celeste.Mod.Vidcutter.Models;
using Microsoft.Xna.Framework;
using Monocle;
using static Celeste.TextMenu;
using static Celeste.TextMenuExt;

namespace Celeste.Mod.Vidcutter.Entities;

public class LevelRow : Button {
    public int Index { get; set; }
    private string LabelIndex => Index >= 0 ? $"{Index + 1}." : "";
    private bool Colored => Index >= 0;
    
    public readonly LevelInAVideo Data;
    public readonly CustomEaseInSubHeader SubHeader;

    public LevelRow(LevelInAVideo levelInAVideo) : base(GetLabel(levelInAVideo)) {
        Index = -1;
        Data = levelInAVideo;
        SubHeader = new CustomEaseInSubHeader(levelInAVideo.VideoName);
        OnEnter += () => SubHeader.FadeVisible = true;
        OnLeave += () => SubHeader.FadeVisible = false;
    }

    private static string GetLabel(LevelInAVideo levelInAVideo) {
        LoggedString lastLog = levelInAVideo.LastLog;
        string completion;
        if (lastLog.Event == "LEVEL COMPLETE") {
            completion = Dialog.Clean("VIDCUTTER_LEVEL_CLEARED");
        } else {
            completion = Dialog.Clean("VIDCUTTER_LEVEL_UNTIL") + $" {lastLog.Room}";
        }
        return $"{levelInAVideo.Level} ({completion})";
    }
    public override void Render(Vector2 position, bool highlighted) {
        float alpha = Container.Alpha;
        Color color;
        if (highlighted) {
            color = Container.HighlightColor;
        } else if (Colored) {
            color = Color.Goldenrod;
        } else {
            color = Color.White;
        }
        color *= alpha;
        Color strokeColor = Color.Black * (alpha * alpha * alpha);
        Vector2 justify = new(0f, 0.5f);
        Vector2 positionIndex = position + new Vector2(-100f, 0f);
        ActiveFont.DrawOutline(LabelIndex, positionIndex, justify, Vector2.One, color, 2f, strokeColor);
        ActiveFont.DrawOutline(Label, position, justify, Vector2.One, color, 2f, strokeColor);
    }
}

public class CustomEaseInSubHeader : SubHeaderExt {
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