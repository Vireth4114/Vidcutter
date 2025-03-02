using System.Buffers.Text;
using Microsoft.Xna.Framework;
using static Celeste.TextMenu;
using static Celeste.TextMenuExt;

namespace Celeste.Mod.Vidcutter;

public class CustomButton : Button {
    public string LabelIndex;
    public bool Colored;

    public CustomButton(string labelIndex, string label) : base(label) {
        LabelIndex = labelIndex;
    }
    
    public override void Render(Vector2 position, bool highlighted) {
        float alpha = Container.Alpha;
        Color color = Disabled ? Color.DarkSlateGray : ((highlighted ? Container.HighlightColor : (Colored ? Color.Goldenrod : Color.White)) * alpha);
        Color strokeColor = Color.Black * (alpha * alpha * alpha);
        Vector2 justify = new Vector2(0f, 0.5f);
        Vector2 positionIndex = position + new Vector2(-100f, 0f);
        ActiveFont.DrawOutline(LabelIndex, positionIndex, justify, Vector2.One, color, 2f, strokeColor);
        ActiveFont.DrawOutline(Label, position, justify, Vector2.One, color, 2f, strokeColor);
    }
}

public class CustomEaseIn : EaseInSubHeaderExt {
    public CustomEaseIn(string label, bool initiallyVisible, TextMenu menu, string icon = null) :
        base(label, initiallyVisible, menu, icon) {}

    public override void Render(Vector2 position, bool highlighted)
    {
        position += Offset;
        float num = Container.Alpha * Alpha;
        Color strokeColor = Color.Black * (num * num * num);
        Vector2 textPosition = position + new Vector2(0f, MathHelper.Max(0f, -16f + HeightExtra));
        Vector2 justify = new Vector2(0f, 0.5f);
        DrawIcon(position, Icon, IconWidth, Height(), IconOutline, Color.White * num, ref textPosition);
        if (Title.Length > 0)
        {
            ActiveFont.DrawOutline(Title, textPosition, justify, Vector2.One * 0.6f, TextColor * num, 2f, strokeColor);
        }
    }
}