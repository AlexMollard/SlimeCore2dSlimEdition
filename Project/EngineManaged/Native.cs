using System;
using System.Runtime.InteropServices;

internal static class Native
{
	// -----------------------------
	// Core / Logging
	// -----------------------------
	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Engine_Log([MarshalAs(UnmanagedType.LPUTF8Str)] string msg);

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
	// Input: keyboard / mouse / window
	// -----------------------------
	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern bool Input_GetKeyDown(Keycode key);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern bool Input_GetKeyReleased(Keycode key);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Input_GetMousePos(out float x, out float y);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Input_GetMouseDelta(out float x, out float y);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern bool Input_GetMouseDown(int button);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Input_GetMouseToWorldPos(out float x, out float y);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Input_GetWindowSize(out float w, out float h);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Input_GetAspectRatio(out float x, out float y);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Input_SetViewportRect(int x, int y, int width, int height);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Input_GetViewportRect(out int x, out int y, out int w, out int h);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Input_SetScroll(float newScroll);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern float Input_GetScroll();

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern bool Input_GetFocus();

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Input_SetFocus(bool focus);

	// -----------------------------
	// ObjectManager helpers
	// -----------------------------
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

	// -----------------------------
	// UI helpers
	// -----------------------------
	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern ulong UI_CreateText([MarshalAs(UnmanagedType.LPUTF8Str)] string text, int fontSize, float x, float y);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void UI_Destroy(ulong id);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void UI_SetText(ulong id, [MarshalAs(UnmanagedType.LPUTF8Str)] string text);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void UI_SetPosition(ulong id, float x, float y);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void UI_SetAnchor(ulong id, float ax, float ay);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void UI_SetColor(ulong id, float r, float g, float b);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void UI_SetVisible(ulong id, bool visible);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void UI_SetLayer(ulong id, int layer);

	// -----------------------------
	// Text / Font helpers
	// -----------------------------
	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern uint Text_CreateTextureFromFontFile([MarshalAs(UnmanagedType.LPUTF8Str)] string fontPath, [MarshalAs(UnmanagedType.LPUTF8Str)] string text, int pixelHeight, out int outWidth, out int outHeight);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr Font_LoadFromFile([MarshalAs(UnmanagedType.LPUTF8Str)] string path);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern void Font_Free(IntPtr font);

	[DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
	internal static extern uint Text_RenderToEntity(IntPtr font, ulong id, [MarshalAs(UnmanagedType.LPUTF8Str)] string text, int pixelHeight);

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

}