using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

internal static class Native
{
	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Engine_Log([MarshalAs(UnmanagedType.LPUTF8Str)] string msg);


	// GAME OBJECTS
	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern ulong Entity_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_Destroy(ulong id);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern bool Entity_IsAlive(ulong id);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Transform_SetPosition(ulong id, float x, float y);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Transform_GetPosition(ulong id, out float x, out float y);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Transform_SetSize(ulong id, float w, float h);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Transform_GetSize(ulong id, out float w, out float h);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Visual_SetColor(ulong id, float r, float g, float b);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Visual_SetLayer(ulong id, int layer);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern int Visual_GetLayer(ulong id);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Visual_SetAnchor(ulong id, float ax, float ay);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Visual_GetAnchor(ulong id, out float ax, out float ay);

	// INPUT
	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern bool Input_GetKeyDown(Keycode key);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern bool Input_GetKeyReleased(Keycode key);

	// TEXT / FONT
	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern uint Text_CreateTextureFromFontFile([MarshalAs(UnmanagedType.LPUTF8Str)] string fontPath, [MarshalAs(UnmanagedType.LPUTF8Str)] string text, int pixelHeight, out int outWidth, out int outHeight);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr Font_LoadFromFile([MarshalAs(UnmanagedType.LPUTF8Str)] string path);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Font_Free(IntPtr font);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern uint Text_RenderToEntity(IntPtr font, ulong id, [MarshalAs(UnmanagedType.LPUTF8Str)] string text, int pixelHeight);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_SetTexture(ulong id, uint texId, int width, int height);

}