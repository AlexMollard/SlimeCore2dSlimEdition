using EngineManaged.Scene;
using System;

namespace EngineManaged.UI;

public class UISlider
{
    public readonly UIImage Background;
    public readonly UIImage Handle;

    public bool Enabled { get; set; } = true;
    public int Layer { get; private set; }
    public bool UseScreenSpace { get; private set; }
    
    private float _width;
    private float _height;
    private float _handleWidth;
    private float _handleHeight;

    private float _value; // 0.0 to 1.0
    public float Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, 0.0f, 1.0f);
            UpdateHandlePosition();
        }
    }

    public event Action<float>? OnValueChanged;

    internal bool IsDragging { get; set; }
    internal bool IsHovered { get; set; }

    private UISlider(UIImage bg, UIImage handle, int layer, float w, float h)
    {
        Background = bg;
        Handle = handle;
        Layer = layer;
        _width = w;
        _height = h;
        _handleWidth = h; 
        _handleHeight = h * 1.5f; 
    }

    public static UISlider Create(float x, float y, float w, float h, float initialValue = 0.5f, int layer = 100, bool useScreenSpace = false)
    {
        var bg = UIImage.Create(x, y, w, h);
        bg.Anchor(0.5f, 0.5f);
        bg.Layer(layer);
        bg.UseScreenSpace(useScreenSpace);
        bg.Color(0.2f, 0.2f, 0.2f); // Dark grey track

        var handleH = h * 2.0f;
        var handleW = h; 
        var handle = UIImage.Create(x, y, handleW, handleH);
        handle.Anchor(0.5f, 0.5f);
        handle.Layer(layer + 1);
        handle.UseScreenSpace(useScreenSpace);
        handle.Color(0.8f, 0.8f, 0.8f); // Light grey handle

        var slider = new UISlider(bg, handle, layer, w, h);
        slider._handleWidth = handleW;
        slider._handleHeight = handleH;
        slider.UseScreenSpace = useScreenSpace;
        slider.Value = initialValue;

        UISystem.Register(slider);

        return slider;
    }

    public void Destroy()
    {
        Background.Destroy();
        Handle.Destroy();
        UISystem.Unregister(this);
    }

    public void SetVisible(bool visible)
    {
        Background.IsVisible(visible);
        Handle.IsVisible(visible);
    }

    public void SetPosition(float x, float y)
    {
        Background.Position = (x, y);
        UpdateHandlePosition();
    }

    private void UpdateHandlePosition()
    {
        var (bx, by) = Background.Position;
        
        float minX = bx - _width / 2.0f + _handleWidth / 2.0f;
        float maxX = bx + _width / 2.0f - _handleWidth / 2.0f;
        
        if (minX > maxX) minX = maxX = bx;

        float hx = minX + (maxX - minX) * _value;
        Handle.Position = (hx, by);
    }

    internal bool ContainsPoint(float mx, float my)
    {
        var (bx, by) = Background.Position;
        var w = _width + _handleWidth; 
        var h = Math.Max(_height, _handleHeight);
        
        return (mx > bx - w / 2 && mx < bx + w / 2 && my > by - h / 2 && my < by + h / 2);
    }
    
    internal void UpdateFromMouse(float mx)
    {
        var (bx, _) = Background.Position;
        float minX = bx - _width / 2.0f + _handleWidth / 2.0f;
        float maxX = bx + _width / 2.0f - _handleWidth / 2.0f;
        
        if (maxX <= minX) return;

        float newVal = (mx - minX) / (maxX - minX);
        float clamped = Math.Clamp(newVal, 0.0f, 1.0f);
        
        if (Math.Abs(clamped - _value) > 0.001f)
        {
            _value = clamped;
            UpdateHandlePosition();
            OnValueChanged?.Invoke(_value);
        }
    }
}
