namespace EngineManaged.UI;

public readonly struct UIImage
{
    public readonly ulong Id;
    public UIImage(ulong id) => Id = id;

    public bool IsValid => Id != 0;

    public static UIImage Create(float x, float y, float w, float h)
    {
        var id = NativeMethods.UI_CreateImage(x, y, w, h);
        return new UIImage(id);
    }

    public void Destroy() { if (Id != 0) NativeMethods.UI_Destroy(Id); }

    public (float x, float y) Position
    {
        get { NativeMethods.UI_GetPosition(Id, out var x, out var y); return (x, y); }
        set => NativeMethods.UI_SetPosition(Id, value.x, value.y);
    }

    public (float w, float h) Size
    {
        get { NativeMethods.UI_GetSize(Id, out var w, out var h); return (w, h); }
        set => NativeMethods.UI_SetSize(Id, value.w, value.h);
    }

    public void Anchor(float x, float y) => NativeMethods.UI_SetAnchor(Id, x, y);

    public void Color(float r, float g, float b) => NativeMethods.UI_SetColor(Id, r, g, b);

    public void IsVisible(bool val) => NativeMethods.UI_SetVisible(Id, val);

    public void Layer(int val) => NativeMethods.UI_SetLayer(Id, val);

    public void UseScreenSpace(bool val) => NativeMethods.UI_SetUseScreenSpace(Id, val);
}
