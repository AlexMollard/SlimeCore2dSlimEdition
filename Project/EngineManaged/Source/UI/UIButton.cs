using EngineManaged.Scene;
using System;

namespace EngineManaged.UI;

public class UIButton : UIElement
{
    public readonly UIImage Background;
    public readonly UIText Label;
    public UIImage? Icon;

    public int Layer { get; private set; }
    public bool UseScreenSpace { get; private set; }

    private Action? _onClick;

    // Visual State
    private float _baseR, _baseG, _baseB;
    private float _hoverR, _hoverG, _hoverB;
    private float _pressR, _pressG, _pressB;
    private float _curR, _curG, _curB;

    internal bool IsHovered { get; set; }
    internal bool IsPressed { get; set; }

    private const float ColorLerp = 0.18f;

    private UIButton(UIImage bg, UIText label, int layer, float r, float g, float b)
    {
        Background = bg;
        Label = label;
        Layer = layer;

        SetBaseColor(r, g, b);
        _curR = r; _curG = g; _curB = b;
        Background.Color(_curR, _curG, _curB);
    }
    
    // ... Create method ...

    public static UIButton Create(string text, float x, float y, float w, float h, float r = 0.2f, float g = 0.2f, float b = 0.2f, int layer = 100, int fontSize = 1, bool useScreenSpace = false)
    {
        // Initial create at 0,0, then Layout updates it
        var bg = UIImage.Create(0, 0, w, h);
        bg.Anchor(0.5f, 0.5f);
        bg.Layer(layer);
        bg.UseScreenSpace(useScreenSpace);
        bg.Color(r, g, b);

        var lbl = UIText.Create(text, fontSize, 0, 0);
        lbl.UseScreenSpace(useScreenSpace);
        lbl.Layer(layer + 1);
        lbl.Anchor(0.5f, 0.5f); 

        var btn = new UIButton(bg, lbl, layer, r, g, b);
        btn.SetUseScreenSpace(useScreenSpace); // Use method instead of property setter
        btn.Width = w;
        btn.Height = h;
        
        // Set Local Position
        btn.SetPosition(x, y);

        UISystem.Register(btn);

        return btn;
    }

    protected override void OnUpdateLayout()
    {
        // Update Native components to match computed World Position
        Background.Position = (WorldX, WorldY);
        UpdateLayoutInternal();
    }

    private void UpdateLayoutInternal()
    {
        var (bx, by) = (WorldX, WorldY);

        if (Icon != null && Icon.Value.IsValid)
        {
            var icon = Icon.Value;
            float iconSize = Height * 0.8f;
            icon.Size = (iconSize, iconSize);
            
            if (IconCentered)
            {
                icon.Position = (bx, by);
                if (RenderLabelOverIcon)
                {
                   Label.Position = (bx + LabelOffset.x, by + LabelOffset.y);
                   Label.IsVisible(IsVisible && IsVisible); // Double check logic
                }
                else
                {
                   Label.IsVisible(false);
                }
            }
            else
            {
                float padding = Height * 0.1f;
                float iconX = (bx - Width / 2.0f) + padding + (iconSize / 2.0f);
                icon.Position = (iconX, by);
                float startOfTextSpace = (bx - Width / 2.0f) + padding + iconSize + padding;
                float remainingWidth = Width - (padding + iconSize + padding);
                float textCenterX = startOfTextSpace + remainingWidth / 2.0f;
                Label.Position = (textCenterX, by);
                Label.IsVisible(IsVisible);
            }
        }
        else
        {
             Label.Position = (bx, by);
             Label.IsVisible(IsVisible);
        }
    }

    public override void Destroy()
    {
        Background.Destroy();
        Label.Destroy();
        if (Icon.HasValue) Icon.Value.Destroy();
        UISystem.Unregister(this);
        base.Destroy();
    }

    // ... Rest of methods ...
    
    public override void SetVisible(bool visible)
    {
        base.SetVisible(visible);
        Background.IsVisible(visible);
        // Label visibility handled in UpdateLayoutInternal based on Icon mode
        if (!visible) Label.IsVisible(false);
        else UpdateLayoutInternal();
        
        if (Icon.HasValue) Icon.Value.IsVisible(visible);
    }

    // ...
    
    // Helper to keep existing checks working (UISystem uses Width/Height from UIElement now?)
    // But UISystem accesses _buttonWidth? 
    // We replaced _buttonWidth with Width property in UIElement.
    
    internal bool ContainsPoint(float mx, float my)
    {
        return (mx > WorldX - Width / 2 && mx < WorldX + Width / 2 && my > WorldY - Height / 2 && my < WorldY + Height / 2);
    }
    
    // ...


    public void SetText(string text)
    {
        Label.Text(text);
    }

    public void SetTexture(IntPtr texturePtr)
    {
        Background.SetTexture(texturePtr);
    }

    public bool IconCentered { get; set; }
    public bool RenderLabelOverIcon { get; set; }
    public (float x, float y) LabelOffset { get; set; } = (0, 0);

    public void SetIcon(IntPtr texturePtr)
    {
        if (Icon == null)
        {
             var (x, y) = Background.Position;
             var newIcon = UIImage.Create(x, y, 0, 0);
             newIcon.UseScreenSpace(UseScreenSpace);
             newIcon.Layer(Layer + 1);
             newIcon.Anchor(0.5f, 0.5f);
             newIcon.Color(1, 1, 1);
             Icon = newIcon;
        }
        
        var icon = Icon.Value;
        icon.SetTexture(texturePtr);
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        var (bx, by) = Background.Position;

        if (Icon != null && Icon.Value.IsValid)
        {
            var icon = Icon.Value;

            // Icon size: 80% of button height, square
            float iconSize = Height * 0.8f;
            icon.Size = (iconSize, iconSize);
            
            if (IconCentered)
            {
                icon.Position = (bx, by);
                
                if (RenderLabelOverIcon)
                {
                    Label.Position = (bx + LabelOffset.x, by + LabelOffset.y);
                    Label.IsVisible(IsVisible);
                    // Ensure label is on top of icon
                    Label.Layer(Layer + 2);
                }
                else
                {
                    // Hide label if icon is centered (assuming icon-only mode)
                    Label.IsVisible(false);
                }
            }
            else
            {
                // Padding
                float padding = Height * 0.1f;
                
                // Icon Position: Left aligned with padding
                // Button Left Edge = bx - Width / 2
                // Icon Center X = Left Edge + padding + iconSize / 2
                float iconX = (bx - Width / 2.0f) + padding + (iconSize / 2.0f);
                icon.Position = (iconX, by);
                
                // Text Position: Centered in remaining space
                // Remaining space starts after icon + padding
                float startOfTextSpace = (bx - Width / 2.0f) + padding + iconSize + padding;
                float remainingWidth = Width - (padding + iconSize + padding);
                float textCenterX = startOfTextSpace + remainingWidth / 2.0f;
                
                Label.Position = (textCenterX, by);
                Label.IsVisible(IsVisible);
            }
        }
        else
        {
             Label.Position = (bx, by);
             Label.IsVisible(IsVisible);
        }
    }

    public (float width, float height) GetTextSize()
    {
        return Label.GetSize();
    }

    public float TextWidth => Label.Width;
    public float TextHeight => Label.Height;

    public void SetUseScreenSpace(bool useScreenSpace)
    {
        UseScreenSpace = useScreenSpace;
        Label.UseScreenSpace(useScreenSpace);
        Background.UseScreenSpace(useScreenSpace);
        if (Icon.HasValue)
        {
            var icon = Icon.Value;
            icon.UseScreenSpace(useScreenSpace);
        }
    }

    public event Action Clicked
    {
        add => _onClick += value;
        remove => _onClick -= value;
    }

    internal void InvokeClick() => _onClick?.Invoke();

    public void SetBaseColor(float r, float g, float b)
    {
        _baseR = r; _baseG = g; _baseB = b;
        _hoverR = Math.Min(1f, r * 1.2f); _hoverG = Math.Min(1f, g * 1.2f); _hoverB = Math.Min(1f, b * 1.2f);
        _pressR = Math.Max(0f, r * 0.8f); _pressG = Math.Max(0f, g * 0.8f); _pressB = Math.Max(0f, b * 0.8f);
    }

    public void SetAlpha(float a)
    {
        Background.Alpha(a);
        Label.Alpha(a);
        if (Icon.HasValue)
        {
            var icon = Icon.Value;
            icon.Alpha(a);
        }
    }

    internal void UpdateColor()
    {
        float targetR = _baseR, targetG = _baseG, targetB = _baseB;

        if (IsPressed) { targetR = _pressR; targetG = _pressG; targetB = _pressB; }
        else if (IsHovered) { targetR = _hoverR; targetG = _hoverG; targetB = _hoverB; }

        _curR = Lerp(_curR, targetR, ColorLerp);
        _curG = Lerp(_curG, targetG, ColorLerp);
        _curB = Lerp(_curB, targetB, ColorLerp);

        Background.Color(_curR, _curG, _curB);
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}