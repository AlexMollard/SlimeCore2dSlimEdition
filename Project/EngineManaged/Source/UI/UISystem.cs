using System.Collections.Generic;

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
        var down = Input.IsMouseDown(Input.MouseButton.Left);

        var hoverIndex = -1;
        var highestLayer = int.MinValue;

        // 1. Detect Hover (considering Layers)
        for (var i = 0; i < _buttons.Count; i++)
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
                
                // Note: We assume standard UI Height of 18.0f for now, matching Game2D default.
                // If FactoryGame uses a different camera size for UI, we might need to parameterize this.
                // However, Game2D.cpp passes m_camera->GetOrthographicSize() to RenderUI.
                // FactoryGame sets camera size to 30.0f (VIEW_H).
                // So we should probably use 30.0f if we are in FactoryGame?
                // Or better, we should check if we can get the current camera size.
                // For now, let's try 30.0f since that's what FactoryGame uses.
                // But wait, StateFactoryMenu uses 18.0f? No, it sets VIEW_H = 30.0f.
                
                // TODO: Get this from the active camera or game settings
                float uiHeight = 30.0f; 
                
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
            for (var i = 0; i < _buttons.Count; i++)
                if (i != hoverIndex) _buttons[i].IsPressed = false;
        }
        else if (!down && _prevMouseDown) // Mouse Up
        {
            for (var i = 0; i < _buttons.Count; i++)
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
                        float uiHeight = 30.0f;
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
        for (var i = 0; i < _buttons.Count; i++) _buttons[i].UpdateColor();

        // 4. Handle Sliders
        for (var i = 0; i < _sliders.Count; i++)
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
                float uiHeight = 30.0f; // TODO: Parameterize
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