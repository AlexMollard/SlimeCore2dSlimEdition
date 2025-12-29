using EngineManaged.Numeric;
using System;
using System.Runtime.InteropServices;

namespace EngineManaged.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ParticleProps_Interop
    {
        public float PosX, PosY;
        public float VelX, VelY;
        public float VelVarX, VelVarY;
        public float ColBeginR, ColBeginG, ColBeginB, ColBeginA;
        public float ColEndR, ColEndG, ColEndB, ColEndA;
        public float SizeBegin;
        public float SizeEnd;
        public float SizeVariation;
        public float LifeTime;
    }

    public struct ParticleProps
    {
        public Vec2 Position;
        public Vec2 Velocity, VelocityVariation;
        public Color ColorBegin, ColorEnd;
        public float SizeBegin, SizeEnd, SizeVariation;
        public float LifeTime;
    }

    public class ParticleSystem : IDisposable
    {
        private bool _isDisposed;

        private IntPtr m_NativeInstance;

        private static class NativeMethods
        {
            [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr ParticleSystem_Create(uint maxParticles);

            [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void ParticleSystem_Destroy(IntPtr system);

            [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void ParticleSystem_OnUpdate(IntPtr system, float ts);

            [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void ParticleSystem_OnRender(IntPtr system);

            [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void ParticleSystem_Emit(IntPtr system, ref ParticleProps_Interop props);

            [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void Scene_RegisterParticleSystem(IntPtr system);

            [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void Scene_UnregisterParticleSystem(IntPtr system);
        }

        public ParticleSystem(uint maxParticles = 10000)
        {
            m_NativeInstance = NativeMethods.ParticleSystem_Create(maxParticles);
            if (m_NativeInstance != IntPtr.Zero)
            {
                NativeMethods.Scene_RegisterParticleSystem(m_NativeInstance);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                //Free managed resources if any
            }
            // Free native resources
            if (m_NativeInstance != IntPtr.Zero)
            {
                NativeMethods.Scene_UnregisterParticleSystem(m_NativeInstance);
                NativeMethods.ParticleSystem_Destroy(m_NativeInstance);
                m_NativeInstance = IntPtr.Zero;
            }

            _isDisposed = true;
        }

        public void OnUpdate(float ts)
        {
            if (m_NativeInstance != IntPtr.Zero)
            {
                NativeMethods.ParticleSystem_OnUpdate(m_NativeInstance, ts);
            }
        }

        public void OnRender()
        {
            if (m_NativeInstance != IntPtr.Zero)
            {
                NativeMethods.ParticleSystem_OnRender(m_NativeInstance);
            }
        }

        public void Emit(ParticleProps props)
        {
            if (m_NativeInstance == IntPtr.Zero) return;

            var interop = new ParticleProps_Interop
            {
                PosX = props.Position.X,
                PosY = props.Position.Y,
                VelX = props.Velocity.X,
                VelY = props.Velocity.Y,
                VelVarX = props.VelocityVariation.X,
                VelVarY = props.VelocityVariation.Y,
                ColBeginR = props.ColorBegin.R,
                ColBeginG = props.ColorBegin.G,
                ColBeginB = props.ColorBegin.B,
                ColBeginA = props.ColorBegin.A,
                ColEndR = props.ColorEnd.R,
                ColEndG = props.ColorEnd.G,
                ColEndB = props.ColorEnd.B,
                ColEndA = props.ColorEnd.A,
                SizeBegin = props.SizeBegin,
                SizeEnd = props.SizeEnd,
                SizeVariation = props.SizeVariation,
                LifeTime = props.LifeTime
            };

            NativeMethods.ParticleSystem_Emit(m_NativeInstance, ref interop);
        }
    }
}
