using System.Runtime.InteropServices;
using System.Security;

[SuppressUnmanagedCodeSecurity]
internal static partial class Native
{
	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Engine_Log([MarshalAs(UnmanagedType.LPUTF8Str)] string msg);

}