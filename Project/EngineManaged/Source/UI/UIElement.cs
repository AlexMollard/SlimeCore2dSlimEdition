using EngineManaged.Numeric;
using System.Collections.Generic;

namespace EngineManaged.UI;

public abstract class UIElement
{
    public UIElement? Parent { get; private set; }
    protected List<UIElement> _children = new();

    // Relative position to parent's content origin
    public float LocalX { get; set; }
    public float LocalY { get; set; }

    public float Width { get; set; }
    public float Height { get; set; }

    public bool IsVisible { get; set; } = true;
    public bool Enabled { get; set; } = true;

    // Computed absolute world position
    public float WorldX { get; protected set; }
    public float WorldY { get; protected set; }

    public virtual void AddChild(UIElement child)
    {
        if (child.Parent != null) child.Parent.RemoveChild(child);
        child.Parent = this;
        _children.Add(child);
        child.UpdateLayout();
    }

    public virtual void RemoveChild(UIElement child)
    {
        if (_children.Remove(child))
        {
            child.Parent = null;
        }
    }

    public void SetPosition(float x, float y)
    {
        LocalX = x;
        LocalY = y;
        UpdateLayout();
    }
    
    // Allows parents to offset their children (e.g. ScrollPanel)
    public virtual (float x, float y) GetContentOrigin()
    {
        return (WorldX, WorldY);
    }

    public virtual void UpdateLayout()
    {
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

        OnUpdateLayout();

        foreach (var child in _children)
        {
            child.UpdateLayout();
        }
    }

    protected virtual void OnUpdateLayout() { }

    public virtual void Destroy()
    {
        foreach (var child in _children.ToArray()) child.Destroy();
        _children.Clear();
    }
    
    public virtual void SetVisible(bool visible)
    {
        IsVisible = visible;
        UpdateLayout(); // Layout might depend on visibility in some systems, but here just propagation
        foreach(var child in _children)
        {
            // If parent is hidden, child is hidden visually, 
            // but we might want to keep child's 'IsVisible' property independent?
            // For simple system: child follows parent visibility behavior usually in rendering.
        }
        OnVisibilityChanged(visible);
    }
    
    protected virtual void OnVisibilityChanged(bool visible) { }
}
