using System.Runtime.InteropServices;

internal static partial class Native
{
	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern ulong ObjectManager_CreateGameObject(float px, float py, float sx, float sy, float r, float g, float b);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern ulong ObjectManager_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern ulong ObjectManager_CreateQuadWithTexture(float px, float py, float sx, float sy, uint texId);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void ObjectManager_Destroy(ulong id);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern bool ObjectManager_IsAlive(ulong id);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern int ObjectManager_GetSize();

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern ulong ObjectManager_GetIdAtIndex(int index);
}