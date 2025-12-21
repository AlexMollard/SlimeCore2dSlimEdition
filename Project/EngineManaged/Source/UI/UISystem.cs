using System.Collections.Generic;

namespace EngineManaged.UI
{
	public static class UISystem
	{
		private static readonly List<UIButton> _buttons = new();
		private static bool _prevMouseDown = false;

		public static void Register(UIButton btn) => _buttons.Add(btn);
		public static void Unregister(UIButton btn) => _buttons.Remove(btn);

		/// <summary>
		/// Clears all buttons. Call this when switching Game Modes!
		/// </summary>
		public static void Clear()
		{
			// Optional: Destroy the native entities too if you want strict cleanup
			// for(int i=0; i<_buttons.Count; i++) _buttons[i].Destroy();
			_buttons.Clear();
		}

		public static void Update()
		{
			var (mx, my) = Input.GetMousePos();
			var down = Input.GetMouseDown(Input.MouseButton.Left);

			int hoverIndex = -1;
			int highestLayer = int.MinValue;

			// 1. Detect Hover (considering Layers)
			for (int i = 0; i < _buttons.Count; i++)
			{
				var b = _buttons[i];
				b.IsHovered = false; // Reset frame

				if (!b.Enabled) continue;

				if (b.ContainsPoint(mx, my))
				{
					if (b.Layer >= highestLayer)
					{
						highestLayer = b.Layer;
						hoverIndex = i;
					}
				}
			}

			if (hoverIndex != -1) _buttons[hoverIndex].IsHovered = true;

			// 2. Handle Click States
			if (down && !_prevMouseDown) // Mouse Down
			{
				if (hoverIndex != -1) _buttons[hoverIndex].IsPressed = true;

				// Unpress others
				for (int i = 0; i < _buttons.Count; i++)
					if (i != hoverIndex) _buttons[i].IsPressed = false;
			}
			else if (!down && _prevMouseDown) // Mouse Up
			{
				for (int i = 0; i < _buttons.Count; i++)
				{
					var b = _buttons[i];
					if (b.IsPressed)
					{
						if (b.Enabled && b.ContainsPoint(mx, my)) b.InvokeClick();
						b.IsPressed = false;
					}
				}
			}

			// 3. Update Visuals
			for (int i = 0; i < _buttons.Count; i++) _buttons[i].UpdateColor();

			_prevMouseDown = down;
		}
	}
}