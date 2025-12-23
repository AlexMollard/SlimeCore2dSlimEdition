using System;
using System.Runtime.InteropServices;
using EngineManaged.Numeric;

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
        private IntPtr m_NativeInstance;

        [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ParticleSystem_Create(uint maxParticles);

        [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
        private static extern void ParticleSystem_Destroy(IntPtr system);

        [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
        private static extern void ParticleSystem_OnUpdate(IntPtr system, float ts);

        [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
        private static extern void ParticleSystem_OnRender(IntPtr system);

        [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
        private static extern void ParticleSystem_Emit(IntPtr system, ref ParticleProps_Interop props);

        [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Scene_RegisterParticleSystem(IntPtr system);

        [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Scene_UnregisterParticleSystem(IntPtr system);

        public ParticleSystem(uint maxParticles = 10000)
        {
            m_NativeInstance = ParticleSystem_Create(maxParticles);
            if (m_NativeInstance != IntPtr.Zero)
                Scene_RegisterParticleSystem(m_NativeInstance);
        }

        public void Dispose()
        {
            if (m_NativeInstance != IntPtr.Zero)
            {
                Scene_UnregisterParticleSystem(m_NativeInstance);
                ParticleSystem_Destroy(m_NativeInstance);
                m_NativeInstance = IntPtr.Zero;
            }
        }

        public void OnUpdate(float ts)
        {
            if (m_NativeInstance != IntPtr.Zero)
                ParticleSystem_OnUpdate(m_NativeInstance, ts);
        }

        public void OnRender()
        {
            if (m_NativeInstance != IntPtr.Zero)
                ParticleSystem_OnRender(m_NativeInstance);
        }

        public void Emit(ParticleProps props)
        {
            if (m_NativeInstance == IntPtr.Zero) return;

            ParticleProps_Interop interop = new ParticleProps_Interop
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

            ParticleSystem_Emit(m_NativeInstance, ref interop);
        }
    }
}
