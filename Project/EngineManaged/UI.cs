using System;

public static class UI
{
	public readonly struct Text
	{
		public readonly ulong Id;
		public Text(ulong id) => Id = id;

		public static Text Create(string text, int fontSize, float x, float y)
		{
			var id = Native.UI_CreateText(text, fontSize, x, y);
			return new Text(id);
		}

		public void Destroy() { if (Id != 0) Native.UI_Destroy(Id); }
		public void SetText(string text) => Native.UI_SetText(Id, text);
		public void SetPosition(float x, float y) => Native.UI_SetPosition(Id, x, y);
		public void SetAnchor(float ax, float ay) => Native.UI_SetAnchor(Id, ax, ay);
		public void SetColor(float r, float g, float b) => Native.UI_SetColor(Id, r, g, b);
		public void SetVisible(bool v) => Native.UI_SetVisible(Id, v);
		public void SetLayer(int layer) => Native.UI_SetLayer(Id, layer);
	}
}