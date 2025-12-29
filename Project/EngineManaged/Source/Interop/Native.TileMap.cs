using System;
using System.Runtime.InteropServices;

internal static partial class Native
{
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr TileMap_Create(int width, int height, float tileSize);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void TileMap_Destroy(IntPtr tileMap);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void TileMap_SetTile(IntPtr tileMap, int x, int y, int layer, IntPtr texturePtr, float r, float g, float b, float a);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void TileMap_UpdateMesh(IntPtr tileMap);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void TileMap_Render(IntPtr tileMap);
}
