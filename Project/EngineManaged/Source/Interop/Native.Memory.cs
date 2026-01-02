using System.Runtime.InteropServices;

namespace SlimeCore;

internal static class UnsafeNativeMethods
{
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
    internal static extern void Memory_PushContext([MarshalAs(UnmanagedType.LPStr)] string context);
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Memory_PopContext();
}
