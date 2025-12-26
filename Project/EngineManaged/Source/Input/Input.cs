using SlimeCore.Source.Input;

internal static class Input
{
    public enum MouseButton : int { Left = 0, Right = 1, Middle = 2 }

    public static (float x, float y) GetMousePos() { Native.Input_GetMouseToWorldPos(out var x, out var y); return (x, y); }
    public static (float x, float y) GetMouseScreenPos() { Native.Input_GetMousePos(out var x, out var y); return (x, y); }
    public static bool GetMouseDown(MouseButton button) => Native.Input_GetMouseDown((int)button);
    public static (float x, float y) GetMouseToWorld() { Native.Input_GetMouseToWorldPos(out var x, out var y); return (x, y); }

    public static bool GetKeyDown(Keycode key) => Native.Input_GetKeyDown(key);
    public static bool GetKeyReleased(Keycode key) => Native.Input_GetKeyReleased(key);


    public static float GetScroll() => Native.Input_GetScroll();

}