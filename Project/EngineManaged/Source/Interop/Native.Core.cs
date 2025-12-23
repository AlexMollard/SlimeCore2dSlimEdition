using System.Runtime.InteropServices;
using System.Security;

[SuppressUnmanagedCodeSecurity]
internal static partial class Native
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