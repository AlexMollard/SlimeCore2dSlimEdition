using System.Runtime.InteropServices;

internal static partial class Native
{
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern ulong UI_CreateText([MarshalAs(UnmanagedType.LPUTF8Str)] string text, int fontSize, float x, float y);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void UI_Destroy(ulong id);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void UI_SetText(ulong id, [MarshalAs(UnmanagedType.LPUTF8Str)] string text);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void UI_SetPosition(ulong id, float x, float y);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void UI_SetAnchor(ulong id, float ax, float ay);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void UI_SetColor(ulong id, float r, float g, float b);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void UI_SetVisible(ulong id, bool visible);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void UI_SetLayer(ulong id, int layer);
}