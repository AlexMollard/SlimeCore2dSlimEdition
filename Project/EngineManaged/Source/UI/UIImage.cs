namespace EngineManaged.UI;

public readonly struct UIImage
{
	public readonly ulong Id;
	public UIImage(ulong id) => Id = id;

	public bool IsValid => Id != 0;

	public static UIImage Create(float x, float y, float w, float h)
	{
		var id = Native.UI_CreateImage(x, y, w, h);
		return new UIImage(id);
	}

	public void Destroy() { if (Id != 0) Native.UI_Destroy(Id); }

	public (float x, float y) Position
	{
		get { Native.UI_GetPosition(Id, out float x, out float y); return (x, y); }
		set => Native.UI_SetPosition(Id, value.x, value.y);
	}

    public (float w, float h) Size
    {
        get { Native.UI_GetSize(Id, out float w, out float h); return (w, h); }
        set => Native.UI_SetSize(Id, value.w, value.h);
    }

	public (float x, float y) Anchor
	{
		set => Native.UI_SetAnchor(Id, value.x, value.y);
	}

	public (float r, float g, float b) Color
	{
		set => Native.UI_SetColor(Id, value.r, value.g, value.b);
	}

	public bool IsVisible
	{
		set => Native.UI_SetVisible(Id, value);
	}

	public int Layer
	{
		set => Native.UI_SetLayer(Id, value);
	}

	public bool UseScreenSpace
	{
		set => Native.UI_SetUseScreenSpace(Id, value);
	}
}
