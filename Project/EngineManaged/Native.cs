using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

internal static class Native
{
	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Engine_Log([MarshalAs(UnmanagedType.LPUTF8Str)] string msg);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern ulong Entity_Create();

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_Destroy(ulong id);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern bool Entity_IsAlive(ulong id);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Transform_SetPosition(ulong id, float x, float y);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Transform_GetPosition(ulong id, out float x, out float y);
}