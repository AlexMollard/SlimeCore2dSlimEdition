using System.Collections.Generic;
using EngineManaged.Scene;

namespace EngineManaged.UI;

public static class UISystem
{
    private static readonly List<UIButton> _buttons = new();
    private static readonly List<UISlider> _sliders = new();
    private static bool _prevMouseDown;

    public static bool IsMouseOverUI { get; private set; }

    public static void Register(UIButton btn) => _buttons.Add(btn);
    public static void Unregister(UIButton btn) => _buttons.Remove(btn);

    public static void Register(UISlider slider) => _sliders.Add(slider);
    public static void Unregister(UISlider slider) => _sliders.Remove(slider);

    /// <summary>
    /// Clears all buttons. Call this when switching Game Modes!
    /// </summary>
    public static void Clear()
    {
        // Optional: Destroy the native entities too if you want strict cleanup
        // for(int i=0; i<_buttons.Count; i++) _buttons[i].Destroy();
        _buttons.Clear();
        _sliders.Clear();
    }

    public static void Update()
    {
        var (mxWorld, myWorld) = Input.GetMousePos();
        var (mxScreen, myScreen) = Input.GetMouseScreenPos();
        bool down = Input.IsMouseDown(Input.MouseButton.Left);

        // Find primary camera size
        float uiHeight = 30.0f; // Default fallback
        foreach (var entity in Scene.Scene.Enumerate())
        {
            if (entity.HasComponent<CameraComponent>())
            {
                var cam = entity.GetComponent<CameraComponent>();
                if (cam.IsPrimary)
                {
                    uiHeight = cam.Size;
                    break;
                }
            }
        }

        int hoverIndex = -1;
        int highestLayer = int.MinValue;

        // 1. Detect Hover (considering Layers)
        for (int i = 0; i < _buttons.Count; i++)
        {
            var b = _buttons[i];
            b.IsHovered = false; // Reset frame

            if (!b.Enabled) continue;

            // Use appropriate coordinate system based on button's screen-space setting
            float mx, my;
            
            if (b.UseScreenSpace)
            {
                // Button is in Pixels (Top-Left origin)
                // Input.GetMouseScreenPos returns Pixels (Top-Left origin)
                mx = mxScreen;
                my = myScreen;
            }
            else
            {
                // Button is in UI World Units (Center origin, Y-up)
                // Input.GetMousePos returns Main World Units (Camera dependent) -> WRONG for UI
                // We need Screen Pixels -> UI World Units
                
                var (uix, uiy) = UIInputHelper.ScreenToUIWorld(mxScreen, myScreen, uiHeight);
                mx = uix;
                my = uiy;
            }

            if (b.ContainsPoint(mx, my))
            {
                if (b.Layer >= highestLayer)
                {
                    highestLayer = b.Layer;
                    hoverIndex = i;
                }
            }
        }

        if (hoverIndex != -1) 
        {
            _buttons[hoverIndex].IsHovered = true;
            IsMouseOverUI = true;
        }
        else
        {
            IsMouseOverUI = false;
        }

        // 2. Handle Click States
        if (down && !_prevMouseDown) // Mouse Down
        {
            if (hoverIndex != -1) _buttons[hoverIndex].IsPressed = true;

            // Unpress others
            for (int i = 0; i < _buttons.Count; i++)
                if (i != hoverIndex) _buttons[i].IsPressed = false;
        }
        else if (!down && _prevMouseDown) // Mouse Up
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                var b = _buttons[i];
                if (b.IsPressed)
                {
                    // Use appropriate coordinate system
                    float mx, my;
                    if (b.UseScreenSpace)
                    {
                        mx = mxScreen;
                        my = myScreen;
                    }
                    else
                    {
                        var (uix, uiy) = UIInputHelper.ScreenToUIWorld(mxScreen, myScreen, uiHeight);
                        mx = uix;
                        my = uiy;
                    }

                    if (b.Enabled && b.ContainsPoint(mx, my)) b.InvokeClick();
                    b.IsPressed = false;
                }
            }
        }

        // 3. Update Visuals
        for (int i = 0; i < _buttons.Count; i++) _buttons[i].UpdateColor();

        // 4. Handle Sliders
        for (int i = 0; i < _sliders.Count; i++)
        {
            var s = _sliders[i];
            if (!s.Enabled) continue;

            // Coordinate conversion (same as buttons)
            float mx, my;
            if (s.UseScreenSpace)
            {
                mx = mxScreen;
                my = myScreen;
            }
            else
            {
                var (uix, uiy) = UIInputHelper.ScreenToUIWorld(mxScreen, myScreen, uiHeight);
                mx = uix;
                my = uiy;
            }

            // Hover
            s.IsHovered = s.ContainsPoint(mx, my);
            if (s.IsHovered) IsMouseOverUI = true;

            // Drag Start
            if (down && !_prevMouseDown && s.IsHovered)
            {
                s.IsDragging = true;
            }
            
            // Drag Update
            if (s.IsDragging)
            {
                if (!down) 
                {
                    s.IsDragging = false;
                }
                else
                {
                    s.UpdateFromMouse(mx);
                }
            }
        }

        _prevMouseDown = down;
    }
}