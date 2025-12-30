using SlimeCore.Source.Input;

internal static class Input
{
    public enum MouseButton : int { Left = 0, Right = 1, Middle = 2 }

    public static (float x, float y) GetMousePos() { SafeNativeMethods.Input_GetMouseToWorldPos(out float x, out float y); return (x, y); }
    public static (float x, float y) GetMouseScreenPos() { SafeNativeMethods.Input_GetMousePos(out float x, out float y); return (x, y); }
    public static bool GetMouseDown(MouseButton button) => SafeNativeMethods.Input_GetMouseDown((int)button);
    public static bool IsMouseDown(MouseButton button) => SafeNativeMethods.Input_IsMouseButtonDown((int)button);
    public static (float x, float y) GetMouseToWorld() { SafeNativeMethods.Input_GetMouseToWorldPos(out float x, out float y); return (x, y); }

    public static bool GetKeyDown(Keycode key) => SafeNativeMethods.Input_GetKeyDown(key);
    public static bool GetKeyReleased(Keycode key) => SafeNativeMethods.Input_GetKeyReleased(key);


    public static float GetScroll() => SafeNativeMethods.Input_GetScroll();

}