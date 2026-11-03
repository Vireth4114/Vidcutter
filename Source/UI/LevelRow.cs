using Celeste.Mod.Vidcutter.Models;
using Microsoft.Xna.Framework;
using static Celeste.TextMenu;

namespace Celeste.Mod.Vidcutter.UI;

public class LevelRow : Button {
    public int Index { get; set; }
    private string LabelIndex => Index >= 0 ? $"{Index + 1}." : "";
    private bool Colored => Index >= 0;
    
    public LevelInAVideo Data { get; }
    public CustomEaseInSubHeader SubHeader { get; }

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