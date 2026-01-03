using EngineManaged.Numeric;
using EngineManaged.Rendering;
using EngineManaged.Scene;
using EngineManaged.UI;
using SlimeCore;
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


    public override void Init()
    {

        UnsafeNativeMethods.Memory_PushContext("ItemRegistry::Initialize");
        SlimeCore.GameModes.Idle.Store.StoreRegistry.Initialize();
        UnsafeNativeMethods.Memory_PopContext();

        ChangeState(new StateIdlePlaying());

        CameraEntity = Entity.Create();
        CameraEntity.AddComponent<TransformComponent>();
        CameraEntity.AddComponent<CameraComponent>();
        var cam = CameraEntity.GetComponent<CameraComponent>();
        cam.IsPrimary = true;
        cam.Zoom = 1.0f;
        cam.Size = 20.0f; // Zoom out to see the whole arena

    }
    public override void Shutdown()
    {
        CameraEntity?.Destroy();
        UISystem.Clear();
    }

    public override void Update(float dt)
    {
        if (_currentState != null) _currentState.Update(this, dt);
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
}