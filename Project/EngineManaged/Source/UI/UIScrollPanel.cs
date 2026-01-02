using System.Collections.Generic;
using System;
using EngineManaged.Scene;

namespace EngineManaged.UI;

public class UIScrollPanel
{
    public readonly UIImage Background;
    private readonly List<UIButton> _children = new();
    
    public float X { get; private set; }
    public float Y { get; private set; }
    public float Width { get; private set; }
    public float Height { get; private set; }
    
    public float ScrollOffset { get; private set; }
    public float ContentHeight { get; set; }
    
    public bool UseScreenSpace { get; private set; }
    public bool IsVisible { get; private set; } = true;
    public bool IsHovered { get; private set; }

    // Store relative positions of children
    private readonly Dictionary<UIButton, (float relX, float relY)> _childOffsets = new();

    private UIScrollPanel(UIImage bg, float x, float y, float w, float h, bool useScreenSpace)
    {
        Background = bg;
        X = x; Y = y; Width = w; Height = h;
        UseScreenSpace = useScreenSpace;
    }

    public static UIScrollPanel Create(float x, float y, float w, float h, int layer = 90, bool useScreenSpace = false)
    {
        var bg = UIImage.Create(x, y, w, h);
        bg.Anchor(0.5f, 0.5f); // Center anchor for panel itself
        bg.Layer(layer);
        bg.UseScreenSpace(useScreenSpace);
        bg.Color(0.2f, 0.2f, 0.2f); // Default dark background
        
        var panel = new UIScrollPanel(bg, x, y, w, h, useScreenSpace);
        UISystem.Register(panel);
        return panel;
    }

    public void AddChild(UIButton btn, float relativeX, float relativeY)
    {
        _children.Add(btn);
        _childOffsets[btn] = (relativeX, relativeY);
        
        // Ensure child matches panel's coordinate space setting
        btn.SetUseScreenSpace(UseScreenSpace);
        
        if (!IsVisible)
        {
            btn.SetVisible(false);
        }
        else
        {
            UpdateLayout();
        }
    }

    public void RemoveChild(UIButton btn)
    {
        _children.Remove(btn);
        _childOffsets.Remove(btn);
    }
    
    public void Clear()
    {
        foreach(var btn in _children) btn.Destroy();
        _children.Clear();
        _childOffsets.Clear();
        ScrollOffset = 0;
    }

    public void Destroy()
    {
        Background.Destroy();
        foreach(var btn in _children) btn.Destroy();
        _children.Clear();
        UISystem.Unregister(this);
    }

    public void SetVisible(bool visible)
    {
        IsVisible = visible;
        Background.IsVisible(visible);
        foreach(var btn in _children)
        {
            // If panel is hidden, hide all children. 
            // If panel is shown, UpdateLayout will decide which children are visible.
            if (!visible) btn.SetVisible(false); 
        }
        if (visible) UpdateLayout();
    }

    public void Update()
    {
        if (!IsVisible) return;

        // Handle Scrolling
        // Check if mouse is over the panel
        var (mxScreen, myScreen) = Input.GetMouseScreenPos();
        float mx = mxScreen, my = myScreen;

        if (!UseScreenSpace)
        {
             // Find primary camera size
             float uiHeight = 30.0f; // Default
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
             
             var (uix, uiy) = UIInputHelper.ScreenToUIWorld(mxScreen, myScreen, uiHeight);
             mx = uix;
             my = uiy;
        }

        // Check bounds
        IsHovered = (mx > X - Width / 2 && mx < X + Width / 2 && my > Y - Height / 2 && my < Y + Height / 2);

        if (IsHovered)
        {
            float scroll = Input.GetScroll();
            if (scroll != 0)
            {
                ScrollOffset -= scroll * 2.0f; // Scroll down (negative) -> Increase offset (move content up)
                
                float maxScroll = Math.Max(0, ContentHeight - Height);
                ScrollOffset = Math.Clamp(ScrollOffset, 0, maxScroll);
                
                UpdateLayout();
            }
        }
    }

    private void UpdateLayout()
    {
        if (!IsVisible) return;

        float topY = Y + Height / 2;
        float bottomY = Y - Height / 2;
        
        // Margin for clipping (so buttons don't pop out instantly)
        // Ideally we would use scissor rects, but for now we just hide if center is out
        float margin = 1.0f; 

        foreach (var btn in _children)
        {
            if (!_childOffsets.TryGetValue(btn, out var offset)) continue;
            
            // Calculate target position
            // Use Y (center) as base, since CreateUI uses center-relative coordinates
            float targetY = Y + offset.relY + ScrollOffset;
            float targetX = X + offset.relX;

            // Check visibility
            bool isVisible = (targetY > bottomY + margin && targetY < topY - margin);
            
            btn.SetVisible(isVisible);
            if (isVisible)
            {
                btn.SetPosition(targetX, targetY);
            }
        }
    }
}
