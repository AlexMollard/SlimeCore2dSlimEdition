using System.Runtime.InteropServices;

internal static partial class Native
{
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern bool Input_GetKeyDown(Keycode key);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern bool Input_GetKeyReleased(Keycode key);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Input_GetMousePos(out float x, out float y);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern bool Input_GetMouseDown(int button);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Input_GetMouseToWorldPos(out float x, out float y);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Input_SetScroll(float v, float h);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern float Input_GetScroll();
}