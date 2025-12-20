internal static class Input
{
	public enum MouseButton : int { Left = 0, Right = 1, Middle = 2 }

	public static (float x, float y) GetMousePos() { Native.Input_GetMousePos(out var x, out var y); return (x, y); }
	public static (float x, float y) GetMouseDelta() { Native.Input_GetMouseDelta(out var x, out var y); return (x, y); }
	public static bool GetMouseDown(MouseButton button) => Native.Input_GetMouseDown((int)button);
	public static (float x, float y) GetMouseToWorld() { Native.Input_GetMouseToWorldPos(out var x, out var y); return (x, y); }

	public static bool GetKeyDown(Keycode key) => Native.Input_GetKeyDown(key);
	public static bool GetKeyReleased(Keycode key) => Native.Input_GetKeyReleased(key);

	public static (float w, float h) GetWindowSize() { Native.Input_GetWindowSize(out var w, out var h); return (w, h); }
	public static (float x, float y) GetAspectRatio() { Native.Input_GetAspectRatio(out var x, out var y); return (x, y); }
	public static void SetViewportRect(int x, int y, int w, int h) => Native.Input_SetViewportRect(x, y, w, h);
	public static (int x, int y, int w, int h) GetViewportRect() { Native.Input_GetViewportRect(out var x, out var y, out var w, out var h); return (x, y, w, h); }

	public static void SetScroll(float s) => Native.Input_SetScroll(s);
	public static float GetScroll() => Native.Input_GetScroll();

	public static bool GetFocus() => Native.Input_GetFocus();
	public static void SetFocus(bool focus) => Native.Input_SetFocus(focus);
}