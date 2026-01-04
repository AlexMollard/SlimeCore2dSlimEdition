using System;
using System.Runtime.InteropServices;
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Interoperability", "CA2101:Specify marshaling for P/Invoke string arguments",
    Justification = "UTF-8 via MarshalAs(UnmanagedType.LPUTF8Str) is explicit")]
internal static partial class NativeMethods
{
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint Text_CreateTextureFromFontFile([MarshalAs(UnmanagedType.LPUTF8Str)] string fontPath, [MarshalAs(UnmanagedType.LPUTF8Str)] string text, int pixelHeight, out int outWidth, out int outHeight);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr Font_LoadFromFile([MarshalAs(UnmanagedType.LPUTF8Str)] string path);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Font_Free(IntPtr font);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint Text_RenderToEntity(IntPtr font, ulong id, [MarshalAs(UnmanagedType.LPUTF8Str)] string text, int pixelHeight);
}