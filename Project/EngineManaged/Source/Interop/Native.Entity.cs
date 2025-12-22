using EngineManaged.Scene;
using System;
using System.Runtime.InteropServices;

internal static partial class Native
{
// -----------------------------
	// Entity lifecycle (Object-level)
	// -----------------------------
	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern ulong Entity_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_Destroy(ulong id);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern bool Entity_IsAlive(ulong id);


	// -----------------------------
	// Entity transform & visual API (single, consistent surface)
	// -----------------------------
	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_SetPosition(ulong id, float x, float y);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_GetPosition(ulong id, out float x, out float y);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_SetSize(ulong id, float w, float h);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_GetSize(ulong id, out float w, out float h);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_SetColor(ulong id, float r, float g, float b);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_SetLayer(ulong id, int layer);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern int Entity_GetLayer(ulong id);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_SetAnchor(ulong id, float ax, float ay);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_GetAnchor(ulong id, out float ax, out float ay);

    // -----------------------------
	// Entity visual helpers (texture / visibility / animation)
	// -----------------------------
	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_SetTexture(ulong id, uint texId, int width, int height);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_SetRender(ulong id, bool value);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern bool Entity_GetRender(ulong id);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_SetFrame(ulong id, int frame);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern int Entity_GetFrame(ulong id);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_AdvanceFrame(ulong id);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_SetSpriteWidth(ulong id, int width);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern int Entity_GetSpriteWidth(ulong id);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_SetHasAnimation(ulong id, bool value);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Entity_SetFrameRate(ulong id, float frameRate);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern float Entity_GetFrameRate(ulong id);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr Texture_Load(string path);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	public static extern void Entity_SetTexturePtr(ulong id, IntPtr texPtr);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr Entity_GetTexturePtr(ulong id);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr Entity_GetRotation(ulong id);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	public static extern void Entity_SetRotation(ulong id, float rotation);
}