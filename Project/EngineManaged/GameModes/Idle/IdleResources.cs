using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.GameModes.Idle
{
    internal class IdleResources
    {
        public static IntPtr TexIron;
        public static IntPtr TexChildLabor;        

        public static IntPtr TexDebug;        
        private static System.Collections.Generic.Dictionary<string, IntPtr> _textureCache = new();

        public static void Unload()
        {
            _textureCache.Clear();
            TexIron = IntPtr.Zero;
        }

        public static IntPtr GetOrCreateTexture(string name, string path)
        {
            if (_textureCache.TryGetValue(name, out nint ptr))
            {
                return ptr;
            }

            ptr = NativeMethods.Resources_LoadTexture(name, path);
            _textureCache[name] = ptr;
            return ptr;
        }

        public static void Load()
        {
            TexDebug = NativeMethods.Resources_LoadTexture("debug", "Textures/debug.png");

            TexIron = NativeMethods.Resources_LoadTexture("iron", "Textures/Idle/ironore.png");
            TexChildLabor = NativeMethods.Resources_LoadTexture("childLabor", "Textures/Idle/childLabor.png");
        }

    }
}
