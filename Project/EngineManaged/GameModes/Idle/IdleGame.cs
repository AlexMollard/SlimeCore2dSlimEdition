using EngineManaged.Numeric;
using EngineManaged.Rendering;
using EngineManaged.Scene;
using EngineManaged.UI;
using SlimeCore;
using SlimeCore.GameModes.Factory;
using SlimeCore.GameModes.Idle;
using SlimeCore.Source.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace GameModes.Idle;

public sealed class IdleGame : GameMode<IdleGame>, IGameMode, IDisposable
{
    private bool _isDisposed;
    internal Entity? CameraEntity;
    public override Random? Rng { get; set; } = new();

    internal ParticleSystem? ParticleSys;
    internal IntPtr TexParticle;
    public override void Init()
    {

        UnsafeNativeMethods.Memory_PushContext("ItemRegistry::Initialize");
        SlimeCore.GameModes.Idle.Store.StoreRegistry.Initialize();
        UnsafeNativeMethods.Memory_PopContext();


        UnsafeNativeMethods.Memory_PushContext("IdleResources::Load");
        IdleResources.Load();
        UnsafeNativeMethods.Memory_PopContext();

        ChangeState(new StateIdlePlaying());

        CameraEntity = Entity.Create();
        CameraEntity.AddComponent<TransformComponent>();
        CameraEntity.AddComponent<CameraComponent>();
        var cam = CameraEntity.GetComponent<CameraComponent>();
        cam.IsPrimary = true;
        cam.Zoom = 1.0f;
        cam.Size = 20.0f; // Zoom out to see the whole arena

        TexParticle = NativeMethods.Resources_LoadTexture("Particle", "Game/Resources/Textures/idle/spark.png");
        ParticleSys = new ParticleSystem(100);
    }
    public override void Shutdown()
    {
        SlimeCore.GameModes.Idle.Store.StoreRegistry.Unload();
        IdleResources.Unload();
        if (ParticleSys != null)
        {
            ParticleSys.Dispose();
            ParticleSys = null;
        }
        CameraEntity?.Destroy();
        UISystem.Clear();
    }

    public override void Update(float dt)
    {
        if (_currentState != null)
        {
            _currentState.Update(this, dt);
            if (ParticleSys != null) ParticleSys.OnUpdate(dt);
        }
    }
    public void InitializeGame()
    {

    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }
        if (disposing)
        {
            ParticleSys?.Dispose();
        }
        _isDisposed = true;
    }

    public override bool InView(Vec2 position)
    {
        throw new NotImplementedException();
    }

    ~IdleGame()
    {
        Dispose(false);
    }

    internal void ClickEffect(Vec2 pos, int count)
    {
        if (ParticleSys == null || Rng == null) return;
        
        var props = new ParticleProps();
        props.Position = pos;
        props.VelocityVariation = new Vec2(8.0f, 8.0f); // Speed variation handled by random direction + speed
        props.ColorBegin = new Color(1, 1, 1, 1.0f);
        props.ColorEnd = new Color(1, 1, 1, 0.0f);
        props.SizeBegin = 0.5f;
        props.SizeEnd = 0.0f;
        props.SizeVariation = 0.3f;
        props.LifeTime = 1.5f;
        
        

        for (int i = 0; i < count; i++)
        {
            float angle = (float)Rng.NextDouble() * 6.28f;
            float speed = (float)Rng.NextDouble() * 8.0f + 2.0f;
            props.Velocity = new Vec2(MathF.Cos(angle), MathF.Sin(angle)) * speed;

            ParticleSys.Emit(props);
        }
    }
}


