namespace EngineManaged.UI;

public readonly struct UIText
{
	public readonly ulong Id;
	public UIText(ulong id) => Id = id;

	public bool IsValid => Id != 0;

	public static UIText Create(string text, int fontSize, float x, float y)
	{
		var id = Native.UI_CreateText(text, fontSize, x, y);
		return new UIText(id);
	}

	public void Destroy() { if (Id != 0) Native.UI_Destroy(Id); }

	public string Text
	{
		set => Native.UI_SetText(Id, value);
	}

	public (float x, float y) Position
	{
		get { Native.UI_GetPosition(Id, out float x, out float y); return (x, y); }
		set => Native.UI_SetPosition(Id, value.x, value.y);
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

	/// <summary>
	/// Gets the width and height of the text in world units.
	/// </summary>
	public (float width, float height) GetSize()
	{
		Native.UI_GetTextSize(Id, out float width, out float height);
		return (width, height);
	}

	/// <summary>
	/// Gets the width of the text in world units.
	/// </summary>
	public float Width => Native.UI_GetTextWidth(Id);

	/// <summary>
	/// Gets the height of the text in world units.
	/// </summary>
	public float Height => Native.UI_GetTextHeight(Id);
}