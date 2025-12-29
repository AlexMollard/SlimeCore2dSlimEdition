namespace EngineManaged.UI;

public readonly struct UIText
{
    public readonly ulong Id;
    public UIText(ulong id) => Id = id;

    public bool IsValid => Id != 0;

    public static UIText Create(string text, int fontSize, float x, float y)
    {
        var id = NativeMethods.UI_CreateText(text, fontSize, x, y);
        return new UIText(id);
    }

    public void Destroy() { if (Id != 0) NativeMethods.UI_Destroy(Id); }

    public void Text(string val) => NativeMethods.UI_SetText(Id, val);

    public (float x, float y) Position
    {
        get { NativeMethods.UI_GetPosition(Id, out var x, out var y); return (x, y); }
        set => NativeMethods.UI_SetPosition(Id, value.x, value.y);
    }

    public void Anchor(float x, float y) => NativeMethods.UI_SetAnchor(Id, x, y);


    public void Color(float r, float g, float b) => NativeMethods.UI_SetColor(Id, r, g, b);


    public void IsVisible(bool val) => NativeMethods.UI_SetVisible(Id, val);


    public void Layer(int val) => NativeMethods.UI_SetLayer(Id, val);


    public void UseScreenSpace(bool val) => NativeMethods.UI_SetUseScreenSpace(Id, val);


    /// <summary>
    /// Gets the width and height of the text in world units.
    /// </summary>
    public (float width, float height) GetSize()
    {
        NativeMethods.UI_GetTextSize(Id, out var width, out var height);
        return (width, height);
    }

    /// <summary>
    /// Gets the width of the text in world units.
    /// </summary>
    public float Width => NativeMethods.UI_GetTextWidth(Id);

    /// <summary>
    /// Gets the height of the text in world units.
    /// </summary>
    public float Height => NativeMethods.UI_GetTextHeight(Id);
}