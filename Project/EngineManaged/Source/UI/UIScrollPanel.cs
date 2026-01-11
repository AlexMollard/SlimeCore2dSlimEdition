using System.Collections.Generic;
using System;
using EngineManaged.Scene;

namespace EngineManaged.UI;

public class UIScrollPanel : UIElement
{
    public readonly UIImage Background;
    
    // Derived properties
    // X, Y, Width, Height are in base UIElement but we need to map constructor args to them
    // Width/Height are public in UIElement
    
    public float ScrollOffset { get; private set; }
    public float ContentHeight { get; set; }
    public int Layer { get; private set; }
    
    public bool UseScreenSpace { get; private set; }
    // IsVisible is in base
    public bool IsHovered { get; private set; }

    private UIImage? _border;

    private UIScrollPanel(UIImage bg, float x, float y, float w, float h, int layer, bool useScreenSpace)
    {
        Background = bg;
        LocalX = x; LocalY = y; Width = w; Height = h;
        Layer = layer;
        UseScreenSpace = useScreenSpace;
    }

    public float X => LocalX;
    public float Y => LocalY;

    public static UIScrollPanel Create(float x, float y, float w, float h, int layer = 90, bool useScreenSpace = false)
    {
        // Background created at 0,0 locally, updated by Layout
        var bg = UIImage.Create(0, 0, w, h);
        bg.Anchor(0.5f, 0.5f); 
        bg.Layer(layer);
        bg.UseScreenSpace(useScreenSpace);
        bg.Color(0.2f, 0.2f, 0.2f); 
        
        var panel = new UIScrollPanel(bg, x, y, w, h, layer, useScreenSpace);
        panel.UpdateLayout(); // Initial sync
        
        UISystem.Register(panel);
        return panel;
    }

    // Override generic AddChild to ensure correct type if needed, or just let base handle it.
    // Base stores UIElement.
    // Existing code expects AddChild(UIButton btn, float relX, float relY).
    // We should maintain that signature for compatibility or refactor calls.
    // refactoring calls is better, but to check compat let's look at signature.
    
    public void AddChild(UIElement element, float x, float y)
    {
        element.LocalX = x;
        element.LocalY = y;
        base.AddChild(element);
        
        // Ensure child matches panel's coordinate space setting
        if (element is UIButton btn) btn.SetUseScreenSpace(UseScreenSpace);
    }

    public override (float x, float y) GetContentOrigin()
    {
        // Children are relative to center + scroll
        // In previous implementation: targetY = Y + offset.relY + ScrollOffset
        return (WorldX, WorldY + ScrollOffset);
    }

    protected override void OnUpdateLayout()
    {
        // Update background position
        Background.Position = (WorldX, WorldY);
        if (_border.HasValue)
        {
             var b = _border.Value;
             b.Position = (WorldX, WorldY);
        }

        // Handle Culling
        float topY = WorldY + Height / 2;
        float bottomY = WorldY - Height / 2;

        foreach (var child in _children)
        {
             // Base UpdateLayout calls child.UpdateLayout() which sets child.WorldX/Y
             // But we need to check bounds AFTER child position is known?
             // Actually, base.UpdateLayout calls OnUpdateLayout BEFORE updating children?
             // Let's check base class... 
             // Base: OnUpdateLayout(); foreach(child) child.UpdateLayout();
             // So child positions are NOT yet updated when we are here.
             
             // Wait, if we want to cull, we need to know where they WILL be.
             // Or we let them update, then check?
             // But base class iterates children. We can't intervene easily unless we override UpdateLayout completely or do it in child?
             // easier to override UpdateLayout completely.
        }
    }

    public override void UpdateLayout()
    {
        // 1. Calculate our World Position
        if (Parent != null)
        {
            var (ox, oy) = Parent.GetContentOrigin();
            WorldX = ox + LocalX;
            WorldY = oy + LocalY;
        }
        else
        {
            WorldX = LocalX;
            WorldY = LocalY;
        }

        // 2. Update our visuals
        Background.Position = (WorldX, WorldY);
        if (_border.HasValue)
        {
             var b = _border.Value;
             b.Position = (WorldX, WorldY);
        }

        // 3. Update Children & Cull
        float topY = WorldY + Height / 2;
        float bottomY = WorldY - Height / 2;
        float margin = 1.0f;

        var origin = GetContentOrigin();

        foreach (var child in _children)
        {
            // Manually update child layout logic or rely on child's UpdateLayout
            // We can just rely on child.UpdateLayout() because it calls Parent.GetContentOrigin()
            child.UpdateLayout();
            
            // Now check bounds
            float childY = child.WorldY;
            bool isVisible = (childY > bottomY + margin && childY < topY - margin);

            // Override visibility for culling
            // But we must respect the child's own "IsVisible" property (e.g. if I manually hid a button)
            // But for now assume all children in scroll list are visible unless culled
            // We need a way to distinguish "UserHidden" vs "CullHidden".
            // Since UIElement has IsVisible property, modifying it overwrites user intent.
            // But for simple scroll panel, we can assume everything is visible.
            // Or we check child.Enabled? No.
            
            // Standard trick: child.SetVisible(isVisible && child.ShouldBeVisible);
            // But we don't have ShouldBeVisible.
            // For now, let's just use SetVisible. 
            // NOTE: This means if code elsewhere hides a child, scrolling heavily might unhide it if we aren't careful.
            // But in this game, items are usually always visible.
            if (IsVisible) // Only show children if panel is visible
            {
               child.SetVisible(isVisible);
            }
            else
            {
               child.SetVisible(false);
            }
        }
    }

    public void EnableButtons(bool enabled)
    {
        // Recursive enable?
        // UIElement has Enabled property
        Enabled = enabled;
        foreach(var child in _children) child.Enabled = enabled;
    }

    public void SetAlpha(float alpha)
    {
        Background.Alpha(alpha);
        if (_border.HasValue)
        {
            var b = _border.Value;
            b.Alpha(alpha);
        }
        foreach (var child in _children)
        {
            // Try to set alpha on children if they support it
            if (child is UIButton btn) btn.SetAlpha(alpha);
            else if (child is UIScrollPanel panel) panel.SetAlpha(alpha);
            else if (child is UILabel label) label.SetAlpha(alpha);
            // Generic UIElement doesn't have SetAlpha, so we might need an interface or base method?
            // For now, handling known types is enough for the reported errors.
        }
    }
    
    // ... Border and other methods ...

    public void SetBorder(float thickness, float r, float g, float b)
    {
        if (_border == null)
        {
            // Initial create at 0,0, updated in UpdateLayout
            var newBorder = UIImage.Create(0, 0, Width + thickness * 2, Height + thickness * 2);
            newBorder.Anchor(0.5f, 0.5f);
            newBorder.Layer(Layer - 1); 
            newBorder.UseScreenSpace(UseScreenSpace);
            _border = newBorder;
        }
        else
        {
            var border = _border.Value;
            border.Size = (Width + thickness * 2, Height + thickness * 2);
        }
        
        var bVal = _border.Value;
        bVal.Color(r, g, b);
        bVal.IsVisible(IsVisible);
        UpdateLayout();
    }
    
    public override void Destroy()
    {
        Background.Destroy();
        if (_border.HasValue) _border.Value.Destroy();
        UISystem.Unregister(this);
        base.Destroy();
    }
    
    // ... Scroll Logic ...
    public void Update()
    {
       if (!IsVisible) return;

        // Handle Scrolling
        var (mxScreen, myScreen) = Input.GetMouseScreenPos();
        float mx = mxScreen, my = myScreen;

        if (!UseScreenSpace)
        {
             // Simplified lookup similar to before
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

        IsHovered = (mx > WorldX - Width / 2 && mx < WorldX + Width / 2 && my > WorldY - Height / 2 && my < WorldY + Height / 2);

        if (IsHovered)
        {
            float scroll = Input.GetScroll();
            if (scroll != 0)
            {
                ScrollOffset -= scroll * 2.0f;
                float maxScroll = Math.Max(0, ContentHeight - Height);
                ScrollOffset = Math.Clamp(ScrollOffset, 0, maxScroll);
                UpdateLayout();
            }
        }
    }
    
    public override void SetVisible(bool visible)
    {
        base.SetVisible(visible);
        Background.IsVisible(visible);
        if (_border.HasValue) _border.Value.IsVisible(visible);
        
        // base.SetVisible just calls UpdateLayout which handles children
    }
    
    public void Clear()
    {
        foreach(var child in _children) child.Destroy();
        _children.Clear();
        ScrollOffset = 0;
    }
}

