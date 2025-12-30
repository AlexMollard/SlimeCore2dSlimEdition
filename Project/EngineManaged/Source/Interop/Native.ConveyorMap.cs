using System;
using System.Runtime.InteropServices;

internal static partial class Native
{
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr ConveyorMap_Create(int width, int height, float tileSize);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void ConveyorMap_Destroy(IntPtr map);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void ConveyorMap_SetConveyor(IntPtr map, int x, int y, int tier, int direction);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void ConveyorMap_RemoveConveyor(IntPtr map, int x, int y);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void ConveyorMap_UpdateMesh(IntPtr map);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void ConveyorMap_Render(IntPtr map, float time);
}
