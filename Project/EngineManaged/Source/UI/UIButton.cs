using System;
using EngineManaged.Scene;

namespace EngineManaged.UI;

public class UIButton
{
	public readonly Entity Background;
	public readonly UIText Label;

	public bool Enabled { get; set; } = true;
	public int Layer { get; private set; }
	public bool UseScreenSpace { get; private set; } = false;
	private float _buttonWidth;
	private float _buttonHeight;

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
		Background.GetComponent<SpriteComponent>().Color = (_curR, _curG, _curB);
	}

	public static UIButton Create(string text, float x, float y, float w, float h, float r = 0.2f, float g = 0.2f, float b = 0.2f, int layer = 100, int fontSize = 1, bool useScreenSpace = false)
	{
		var bg = SceneFactory.CreateQuad(x, y, w, h, r, g, b, layer);
		bg.GetComponent<TransformComponent>().Anchor = (0.5f, 0.5f);

		var lbl = UIText.Create(text, fontSize, x, y);
		lbl.UseScreenSpace = useScreenSpace;
		lbl.Layer = layer + 1;
		lbl.Anchor = (0.5f, 0.5f); // Center the text

		// Use text width to properly center text
		var (textWidth, textHeight) = lbl.GetSize();
		// Text is already centered at x, y, so no offset needed

		var btn = new UIButton(bg, lbl, layer, r, g, b);
		btn.UseScreenSpace = useScreenSpace;
		btn._buttonWidth = w;
		btn._buttonHeight = h;

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
		Background.GetComponent<SpriteComponent>().IsVisible = visible;
		Label.IsVisible = visible;
	}

	public void SetText(string text)
	{
		Label.Text = text;
	}

	/// <summary>
	/// Gets the width and height of the button's text label.
	/// </summary>
	public (float width, float height) GetTextSize()
	{
		return Label.GetSize();
	}

	/// <summary>
	/// Gets the width of the button's text label.
	/// </summary>
	public float TextWidth => Label.Width;

	/// <summary>
	/// Gets the height of the button's text label.
	/// </summary>
	public float TextHeight => Label.Height;

	public void SetPosition(float x, float y)
	{
		Background.GetComponent<TransformComponent>().Position = (x, y);
		Label.Position = (x, y);
	}

	public void SetUseScreenSpace(bool useScreenSpace)
	{
		UseScreenSpace = useScreenSpace;
		Label.UseScreenSpace = useScreenSpace;
		// Note: Entity (Background) doesn't have screen-space support yet
		// This would need to be added to Entity if needed
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
		if (UseScreenSpace)
		{
			// For screen-space buttons, use the label's position (which is in screen space)
			// and stored button dimensions (in screen pixels)
			var (lx, ly) = Label.Position;
			
			// Check if mouse is within button bounds (using screen coordinates)
			// Button is centered at label position with stored dimensions
			return (mx > lx - _buttonWidth / 2 && mx < lx + _buttonWidth / 2 && 
			        my > ly - _buttonHeight / 2 && my < ly + _buttonHeight / 2);
		}
		else
		{
			// World space: use background position and size
			var (bx, by) = Background.GetComponent<TransformComponent>().Position;
			var (w, h) = Background.GetComponent<TransformComponent>().Scale;
			return (mx > bx - w / 2 && mx < bx + w / 2 && my > by - h / 2 && my < by + h / 2);
		}
	}

	internal void UpdateColor()
	{
		float targetR = _baseR, targetG = _baseG, targetB = _baseB;

		if (IsPressed) { targetR = _pressR; targetG = _pressG; targetB = _pressB; }
		else if (IsHovered) { targetR = _hoverR; targetG = _hoverG; targetB = _hoverB; }

		_curR = Lerp(_curR, targetR, ColorLerp);
		_curG = Lerp(_curG, targetG, ColorLerp);
		_curB = Lerp(_curB, targetB, ColorLerp);

		Background.GetComponent<SpriteComponent>().Color = (_curR, _curG, _curB);
	}

	private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}