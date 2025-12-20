using System;
using System.Collections.Generic;

public static class UI
{
	// Internal button registry for click handling
	private static readonly List<Button> _buttons = new List<Button>();
	private static bool _prevMouseDown = false;

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

	public sealed class Button
	{
		public Entity Background { get; }
		public Text Label { get; }
		private Action _onClick;
		public bool Enabled { get; set; } = true;
		public int Layer { get; private set; }

		// UX color states
		private float baseR, baseG, baseB;
		private float hoverR, hoverG, hoverB;
		private float pressR, pressG, pressB;
		private float curR, curG, curB;
		private bool _hovered = false;
		private bool _pressed = false;

		private const float ColorLerp = 0.18f; // smoothing factor (0..1)

		private Button(Entity bg, Text label, int layer, float r, float g, float b)
		{
			Background = bg;
			Label = label;
			Layer = layer;

			baseR = r; baseG = g; baseB = b;
			// default hover/press adjustments (hover slightly brighter, press slightly darker)
			hoverR = MathF.Min(1f, baseR * 1.12f);
			hoverG = MathF.Min(1f, baseG * 1.12f);
			hoverB = MathF.Min(1f, baseB * 1.12f);

			pressR = MathF.Max(0f, baseR * 0.86f);
			pressG = MathF.Max(0f, baseG * 0.86f);
			pressB = MathF.Max(0f, baseB * 0.86f);

			curR = baseR; curG = baseG; curB = baseB;
			Background.SetColor(curR, curG, curB);
		}

		/// <summary>Create a button composed of a quad (background) and UI text (label).</summary>
		public static Button Create(string text, float x, float y, float w, float h, float r = 0.2f, float g = 0.2f, float b = 0.2f, int layer = 0, int fontSize = 42)
		{
			var bg = Entity.CreateQuad(x, y, w, h, r, g, b, 0.5f, 0.5f, layer);
			var lbl = Text.Create(text, fontSize, x, y);
			lbl.SetAnchor(0.5f, 0.5f);
			lbl.SetLayer(layer + 1);
			var btn = new Button(bg, lbl, layer, r, g, b);
			_buttons.Add(btn);
			return btn;
		}

		public void Destroy() { Background.Destroy(); Label.Destroy(); _buttons.Remove(this); }
		public void SetText(string text) => Label.SetText(text);
		public void SetPosition(float x, float y) { Background.SetPosition(x, y); Label.SetPosition(x, y); }
		public void SetSize(float w, float h) => Background.SetSize(w, h);
		public void SetColor(float r, float g, float b)
		{
			baseR = r; baseG = g; baseB = b;
			hoverR = MathF.Min(1f, baseR * 1.12f);
			hoverG = MathF.Min(1f, baseG * 1.12f);
			hoverB = MathF.Min(1f, baseB * 1.12f);
			pressR = MathF.Max(0f, baseR * 0.86f);
			pressG = MathF.Max(0f, baseG * 0.86f);
			pressB = MathF.Max(0f, baseB * 0.86f);
		}
		public void SetHoverColor(float r, float g, float b) { hoverR = r; hoverG = g; hoverB = b; }
		public void SetPressColor(float r, float g, float b) { pressR = r; pressG = g; pressB = b; }
		public void SetVisible(bool v) { Background.SetVisible(v); Label.SetVisible(v); }
		public void SetLayer(int layer) { Background.SetLayer(layer); Label.SetLayer(layer + 1); Layer = layer; }

		public event Action Clicked
		{
			add => _onClick += value;
			remove => _onClick -= value;
		}

		internal bool ContainsPoint(float mx, float my)
		{
			var (bx, by) = Background.GetPosition();
			var (w, h) = Background.GetSize();
			return (mx > bx - w / 2 && mx < bx + w / 2 && my > by - h / 2 && my < by + h / 2);
		}

		internal void InvokeClick() => _onClick?.Invoke();
		
		internal void SetHovered(bool hovered)
		{
			_hovered = hovered;
		}

		internal void SetPressed(bool pressed)
		{
			_pressed = pressed;
		}

		internal bool GetPressed() => _pressed;

		internal void TickColor()
		{
			float targetR = baseR, targetG = baseG, targetB = baseB;
			if (_pressed)
			{
				targetR = pressR; targetG = pressG; targetB = pressB;
			}
			else if (_hovered)
			{
				targetR = hoverR; targetG = hoverG; targetB = hoverB;
			}

			// smooth transition
			curR = Lerp(curR, targetR, ColorLerp);
			curG = Lerp(curG, targetG, ColorLerp);
			curB = Lerp(curB, targetB, ColorLerp);
			Background.SetColor(curR, curG, curB);
		}

		private static float Lerp(float a, float b, float t) => a + (b - a) * t;
	}

	/// <summary>Call once per-frame to detect button clicks and invoke handlers.</summary>
	public static void Update()
	{
		var (mx, my) = Input.GetMousePos();
		var down = Input.GetMouseDown(Input.MouseButton.Left);

		// Determine hovered (top-most) button under mouse for visual state
		int hoverIndex = -1; int hoverLayer = int.MinValue;
		for (int i = 0; i < _buttons.Count; i++)
		{
			var b = _buttons[i];
			if (!b.Enabled) { b.SetHovered(false); continue; }
			var isHover = b.ContainsPoint(mx, my);
			if (isHover && b.Layer >= hoverLayer) { hoverLayer = b.Layer; hoverIndex = i; }
			// clear hovered initially; we'll set true only on the top-most one below
			b.SetHovered(false);
		}
		if (hoverIndex != -1) _buttons[hoverIndex].SetHovered(true);

		// Handle press / release and click logic
		if (down && !_prevMouseDown)
		{
			// mouse pressed - set pressed on top-most hovered button
			if (hoverIndex != -1) _buttons[hoverIndex].SetPressed(true);
			// clear other pressed states
			for (int i = 0; i < _buttons.Count; i++) if (i != hoverIndex) _buttons[i].SetPressed(false);
		}
		else if (!down && _prevMouseDown)
		{
			// mouse released - invoke click on the button that was pressed if the cursor is still over it
			for (int i = 0; i < _buttons.Count; i++)
			{
				var b = _buttons[i];
				if (b.GetPressed())
				{
					if (b.Enabled && b.ContainsPoint(mx, my)) b.InvokeClick();
					b.SetPressed(false);
				}
			}
		}

		// Tick colors for smoothing
		for (int i = 0; i < _buttons.Count; i++) _buttons[i].TickColor();

		_prevMouseDown = down;
	}
}