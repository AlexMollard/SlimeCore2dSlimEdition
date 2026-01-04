using System;
using System.Runtime.InteropServices;

internal static partial class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchQuad
    {
        public float X, Y;
        public float W, H;
        public float R, G, B, A;
        public IntPtr TexturePtr;
    }

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_DrawBatch([In] BatchQuad[] quads, int count);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_BeginScenePrimary();

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_EndScene();
}
