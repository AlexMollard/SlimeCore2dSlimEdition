using System;

namespace EngineManaged.UI;

public class UILabel : UIElement
{
    public readonly UIText TextComponent;

    private UILabel(UIText text, int layer, bool useScreenSpace)
    {
        TextComponent = text;
        // Initial defaults
        Width = text.Width;
        Height = text.Height;
    }

    public static UILabel Create(string text, int fontSize, float x, float y, int layer = 100, bool useScreenSpace = false)
    {
        var txt = UIText.Create(text, fontSize, x, y);
        txt.UseScreenSpace(useScreenSpace);
        txt.Layer(layer);
        
        var label = new UILabel(txt, layer, useScreenSpace);
        label.LocalX = x;
        label.LocalY = y;
        label.UseScreenSpace(useScreenSpace);
        
        label.UpdateLayout(); // Initial sync
        return label;
    }

    protected override void OnUpdateLayout()
    {
        TextComponent.Position = (WorldX, WorldY);
        // Update size just in case text changed?
        // Width = TextComponent.Width;
        // Height = TextComponent.Height;
    }

    public void SetText(string text)
    {
        TextComponent.Text(text);
        // Refresh dimensions?
    }

    public void SetColor(float r, float g, float b) => TextComponent.Color(r, g, b);
    public void SetScale(float scale) => TextComponent.Scale(scale);
    public void SetAnchor(float x, float y) => TextComponent.Anchor(x, y);

    public override void Destroy()
    {
        TextComponent.Destroy();
        base.Destroy();
    }
    
    public float TextWidth => TextComponent.Width;
    public float TextHeight => TextComponent.Height;

    public void UseScreenSpace(bool val)
    {
         TextComponent.UseScreenSpace(val);
    }
    
    public void SetAlpha(float a) => TextComponent.Alpha(a);

    public override void SetVisible(bool visible)
    {
        base.SetVisible(visible);
        TextComponent.IsVisible(visible);
    }

    public void SetClipRect(float x, float y, float w, float h)
    {
        TextComponent.SetClipRect(x, y, w, h);
    }
}
