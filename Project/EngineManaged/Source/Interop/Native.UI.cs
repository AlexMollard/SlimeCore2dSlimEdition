using System;
using System.Runtime.InteropServices;
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Interoperability", "CA2101:Specify marshaling for P/Invoke string arguments",
    Justification = "UTF-8 via MarshalAs(UnmanagedType.LPUTF8Str) is explicit")]
internal static partial class NativeMethods
{
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern ulong UI_CreateText([MarshalAs(UnmanagedType.LPUTF8Str)] string text, int fontSize, float x, float y);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern ulong UI_CreateImage(float x, float y, float w, float h);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void UI_Destroy(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void UI_SetText(ulong id, [MarshalAs(UnmanagedType.LPUTF8Str)] string text);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void UI_SetPosition(ulong id, float x, float y);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void UI_GetPosition(ulong id, out float x, out float y);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void UI_SetSize(ulong id, float w, float h);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void UI_GetSize(ulong id, out float w, out float h);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void UI_SetAnchor(ulong id, float ax, float ay);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void UI_SetColor(ulong id, float r, float g, float b);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void UI_SetAlpha(ulong id, float a);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void UI_SetVisible(ulong id, bool visible);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void UI_SetLayer(ulong id, int layer);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void UI_SetTexture(ulong id, IntPtr texturePtr);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void UI_SetUseScreenSpace(ulong id, bool useScreenSpace);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void UI_SetWrapWidth(ulong id, float wrapWidth);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void UI_GetTextSize(ulong id, out float width, out float height);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern float UI_GetTextWidth(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern float UI_GetTextHeight(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Input_GetViewportRect(out int x, out int y, out int w, out int h);
}