using System.Runtime.InteropServices;
using System.Security;

[SuppressUnmanagedCodeSecurity]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Interoperability", "CA2101:Specify marshaling for P/Invoke string arguments",
    Justification = "UTF-8 via MarshalAs(UnmanagedType.LPUTF8Str) is explicit")]
internal static partial class SafeNativeMethods
{
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Engine_Log([MarshalAs(UnmanagedType.LPUTF8Str)] string msg);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Engine_LogTrace([MarshalAs(UnmanagedType.LPUTF8Str)] string msg);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Engine_LogInfo([MarshalAs(UnmanagedType.LPUTF8Str)] string msg);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Engine_LogWarn([MarshalAs(UnmanagedType.LPUTF8Str)] string msg);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Engine_LogError([MarshalAs(UnmanagedType.LPUTF8Str)] string msg);

}