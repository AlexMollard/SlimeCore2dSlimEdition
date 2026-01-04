using System.Runtime.InteropServices;

internal static partial class NativeMethods
{
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern ulong Scene_CreateGameObject(float px, float py, float sx, float sy, float r, float g, float b);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern ulong Scene_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern ulong Scene_CreateQuadWithTexture(float px, float py, float sx, float sy, uint texId);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Scene_Destroy(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Scene_IsAlive(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Scene_GetEntityCount();

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern ulong Scene_GetEntityIdAtIndex(int index);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Scene_SetGravity(float x, float y);
}
