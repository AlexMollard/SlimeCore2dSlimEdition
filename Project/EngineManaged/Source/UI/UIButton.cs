using System;
using EngineManaged.Scene;

namespace EngineManaged.UI;

public class UIButton
{
	public readonly Entity Background;
	public readonly UIText Label;

	public bool Enabled { get; set; } = true;
	public int Layer { get; private set; }

	private Action? _onClick;

	// Visual State
	private float _baseR, _baseG, _baseB;
	private float _hoverR, _hoverG, _hoverB;
	private float _pressR, _pressG, _pressB;
	private float _curR, _curG, _curB;

	internal bool IsHovered { get; set; }
	internal bool IsPressed { get; set; }

	private const float ColorLerp = 0.18f;

	private UIButton(Entity bg, UIText label, int layer, float r, float g, float b)
	{
		Background = bg;
		Label = label;
		Layer = layer;

		SetBaseColor(r, g, b);
		_curR = r; _curG = g; _curB = b;
		Background.SetColor(_curR, _curG, _curB);
	}

	public static UIButton Create(string text, float x, float y, float w, float h, float r = 0.2f, float g = 0.2f, float b = 0.2f, int layer = 100, int fontSize = 1)
	{
		var bg = SceneFactory.CreateQuad(x, y, w, h, r, g, b, layer);
		bg.SetAnchor(0.5f, 0.5f);

		var lbl = UIText.Create(text, fontSize, x, y);
		lbl.SetPosition(x - (w / 2), y - (h / 6)); // Slight padding
		lbl.SetLayer(layer + 1);

		var btn = new UIButton(bg, lbl, layer, r, g, b);

		UISystem.Register(btn);

		return btn;
	}

	public void Destroy()
	{
		Background.Destroy();
		Label.Destroy();
		UISystem.Unregister(this);
	}

	// ---------------------------------------------------------------------
	// Wrapper Methods (Fixes 'does not contain definition' errors)
	// ---------------------------------------------------------------------

	public void SetVisible(bool visible)
	{
		Background.IsVisible = visible;
		Label.SetVisible(visible);
	}

	public void SetText(string text)
	{
		Label.Text = text;
	}

	public void SetPosition(float x, float y)
	{
		Background.SetPosition(x, y);
		Label.SetPosition(x, y);
	}

	// ---------------------------------------------------------------------
	// Events & Visuals
	// ---------------------------------------------------------------------

	public event Action Clicked
	{
		add => _onClick += value;
		remove => _onClick -= value;
	}

	internal void InvokeClick() => _onClick?.Invoke();

	public void SetBaseColor(float r, float g, float b)
	{
		_baseR = r; _baseG = g; _baseB = b;
		_hoverR = Math.Min(1f, r * 1.2f); _hoverG = Math.Min(1f, g * 1.2f); _hoverB = Math.Min(1f, b * 1.2f);
		_pressR = Math.Max(0f, r * 0.8f); _pressG = Math.Max(0f, g * 0.8f); _pressB = Math.Max(0f, b * 0.8f);
	}

	internal bool ContainsPoint(float mx, float my)
	{
		var (bx, by) = Background.GetPosition();
		var (w, h) = Background.GetSize();
		return (mx > bx - w / 2 && mx < bx + w / 2 && my > by - h / 2 && my < by + h / 2);
	}

	internal void UpdateColor()
	{
		float targetR = _baseR, targetG = _baseG, targetB = _baseB;

		if (IsPressed) { targetR = _pressR; targetG = _pressG; targetB = _pressB; }
		else if (IsHovered) { targetR = _hoverR; targetG = _hoverG; targetB = _hoverB; }

		_curR = Lerp(_curR, targetR, ColorLerp);
		_curG = Lerp(_curG, targetG, ColorLerp);
		_curB = Lerp(_curB, targetB, ColorLerp);

		Background.SetColor(_curR, _curG, _curB);
	}

	private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}