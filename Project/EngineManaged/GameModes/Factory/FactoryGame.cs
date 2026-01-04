using EngineManaged.Numeric;
using SlimeCore.GameModes.Factory.Actors;
using SlimeCore.GameModes.Factory.States;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.Source.Common;
using SlimeCore.Source.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SlimeCore.GameModes.Factory;

public sealed class FactoryGame : GameMode<FactoryGame>, IGameMode, IDisposable
{
    public override Random? Rng { get; set; }

    public FactorySettings Settings { get; set; }

    public FactoryWorld? World { get; set; }

    public FactoryActorManager? ActorManager { get; set; }

    public ConveyorSystem? ConveyorSystem;
    public BuildingSystem? BuildingSystem;

    // Viewport settings
    public const int VIEW_W = 40;
    public const int VIEW_H = 30;

    public const int MAX_VIEW_W = 200;
    public const int MAX_VIEW_H = 150;

    [SetsRequiredMembers]
    public FactoryGame(FactorySettings settings)
    {
        Settings = settings;
        InitializeGame();
    }

    public void InitializeGame()
    {
        UnsafeNativeMethods.Memory_PushContext("FactoryGame::InitializeGame");
        // Ensure resources are loaded before items
        UnsafeNativeMethods.Memory_PushContext("FactoryResources::Load");
        FactoryResources.Load();
        UnsafeNativeMethods.Memory_PopContext();

        UnsafeNativeMethods.Memory_PushContext("ItemRegistry::Initialize");
        SlimeCore.GameModes.Factory.Items.ItemRegistry.Initialize();
        UnsafeNativeMethods.Memory_PopContext();

        UnsafeNativeMethods.Memory_PushContext("FactoryWorld::New");
        World = new FactoryWorld(Settings.WorldWidth, Settings.WorldHeight, FactoryTerrain.Grass, Settings.InitialZoom, Settings.WorldBudget);
        UnsafeNativeMethods.Memory_PopContext();

        UnsafeNativeMethods.Memory_PushContext("ActorManager::New");
        ActorManager = new FactoryActorManager(Settings.ActorBudget);
        UnsafeNativeMethods.Memory_PopContext();

        Rng = new Random(Settings.Seed);
        UnsafeNativeMethods.Memory_PopContext();
    }

    public override bool InView(Vec2 position)
    {
        float half = MAX_VIEW_W * 0.5f;
        float left = Camera.X - half;
        float right = Camera.X + half;
        float bottom = Camera.Y - half;
        float top = Camera.Y + half;

        return
            position.X >= left && 
            position.X <= right &&
            position.Y >= bottom && 
            position.Y <= top;
    }


    public override void Init()
    {
        SQLitePCL.Batteries.Init();
        ChangeState(new StateFactoryMenu());
    }

    public override void Shutdown()
    {
        Logger.Info("FactoryGame.Shutdown called");
        World?.Destroy();
        ActorManager?.Destroy();
        FactoryResources.Unload();
    }

    public override void Update(float dt)
    {
        if (_currentState != null) _currentState.Update(this, dt);
    }

    public override void Draw()
    {
        if (_currentState != null) _currentState.Draw(this);
    }

    public void Dispose()
    {
        if (ConveyorSystem != null)
        {
            ConveyorSystem.Dispose();
            ConveyorSystem = null;
        }

        if (BuildingSystem != null)
        {
            BuildingSystem.Dispose();
            BuildingSystem = null;
        }
        World?.Destroy();
    }
}
