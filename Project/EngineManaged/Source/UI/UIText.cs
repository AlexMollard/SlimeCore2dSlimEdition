namespace EngineManaged.UI
{
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

		public void SetPosition(float x, float y) => Native.UI_SetPosition(Id, x, y);
		public void SetAnchor(float ax, float ay) => Native.UI_SetAnchor(Id, ax, ay);
		public void SetColor(float r, float g, float b) => Native.UI_SetColor(Id, r, g, b);
		public void SetVisible(bool v) => Native.UI_SetVisible(Id, v);
		public void SetLayer(int layer) => Native.UI_SetLayer(Id, layer);
	}
}