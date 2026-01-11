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

    private float _scrollTarget;

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
            
            // Culling Logic: Treat Child as Box, not Point.
            // Child bounds: WorldY +/- Height/2
            float childH = child.Height;
            float childY = child.WorldY;
            float childTop = childY + childH / 2.0f;
            float childBottom = childY - childH / 2.0f;
            
            // Check intersection (AABB overlap)
            // Visible if: ChildBottom < TopY AND ChildTop > BottomY
            // Add a small margin to be safe
            bool isVisible = (childBottom < topY - margin) && (childTop > bottomY + margin);

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
               
               // Apply Clip Rect (Only needs to happen if visible, but safe to call)
               if (isVisible) ApplyClipRect(child);
            }
            else
            {
               child.SetVisible(false);
            }
        }
    }

    private float GetUIHeight()
    {
        float uiHeight = 30.0f; // Default if no camera found
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
        return uiHeight;
    }

    private void ApplyClipRect(UIElement child)
    {
        // Calculate Screen Rect of this panel
        float sx, sy, sw, sh;

        if (UseScreenSpace)
        {
             // WorldX, WorldY are in Pixels (Center)
             sx = WorldX - Width / 2.0f;
             sy = WorldY - Height / 2.0f;
             sw = Width;
             sh = Height;
        }
        else
        {
             // Convert UI World -> Screen Pixels
             // We need Viewport Size
             NativeMethods.Input_GetViewportRect(out int vx, out int vy, out int vw, out int vh);
             float vpW = vw > 0 ? vw : 1920.0f;
             float vpH = vh > 0 ? vh : 1080.0f;
             
             // We need UI Height (Camera Size)
             float uiHeight = GetUIHeight();
             
             // Aspect
             float aspect = vpH > 0 ? vpW / vpH : 16.0f/9.0f;
             float uiWidth = uiHeight * aspect;
             
             // Conversion logic (Horizontal):
             // uiX = (screenX / vpW) * uiWidth - (uiWidth * 0.5f);
             // screenX = (uiX + uiWidth * 0.5f) / uiWidth * vpW;
             
             float uiX = WorldX; // Center
             float uiY = WorldY; // Center
             
             float centerX = (uiX + uiWidth * 0.5f) / uiWidth * vpW;
             
             // Conversion logic (Vertical):
             // uiY = (uiHeight * 0.5f) - (screenY / vpH) * uiHeight;
             // screenY = ((uiHeight * 0.5f) - uiY) / uiHeight * vpH;
             
             float centerY = ((uiHeight * 0.5f) - uiY) / uiHeight * vpH;
             
             // Size
             // uiW -> screenW
             // screenW = uiW / uiWidth * vpW
             float screenW = Width / uiWidth * vpW;
             float screenH = Height / uiHeight * vpH;

             sx = centerX - screenW / 2.0f;
             sy = centerY - screenH / 2.0f;
             sw = screenW;
             sh = screenH;
        }

        if (child is UIButton btn) btn.SetClipRect(sx, sy, sw, sh);
        else if (child is UILabel lbl) lbl.SetClipRect(sx, sy, sw, sh);
        else if (child is UIScrollPanel pnl) pnl.SetClipRect(sx, sy, sw, sh); // Nested?
    }
    
    // Add SetClipRect for nested scroll panels if needed, though they manage their own children
    public void SetClipRect(float x, float y, float w, float h)
    {
         // UIScrollPanel itself doesn't render much except bg/border
         Background.SetClipRect(x, y, w, h);
         if (_border.HasValue) _border.Value.SetClipRect(x, y, w, h);
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
             // Use robust camera height detection
             float uiHeight = GetUIHeight();
             
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
                // Scroll Speed Tuning:
                // Use a multiplier that feels good for both mouse wheels and trackpads.
                // Removed the minimum step clamping to allow for smooth, small adjustments.
                
                float step = scroll * 5.0f; // Adjusted multiplier for smoother control

                // For normal scroll (Wheel Down -> Negative), we want to increase Target to scroll DOWN (move content UP)
                _scrollTarget -= step; 
                
                float maxScroll = Math.Max(0, ContentHeight - Height);
                _scrollTarget = Math.Clamp(_scrollTarget, 0, maxScroll);
            }
        }

        // Smoothly interpolate ScrollOffset towards _scrollTarget
        if (Math.Abs(ScrollOffset - _scrollTarget) > 0.01f)
        {
            ScrollOffset = ScrollOffset + (_scrollTarget - ScrollOffset) * 0.2f; 
            
            // Snap when close
            if (Math.Abs(ScrollOffset - _scrollTarget) < 0.01f) ScrollOffset = _scrollTarget;
            
            UpdateLayout();
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
        _scrollTarget = 0;
    }
}

