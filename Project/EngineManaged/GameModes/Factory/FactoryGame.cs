using SlimeCore.GameModes.Factory.Actors;
using SlimeCore.GameModes.Factory.States;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.Source.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SlimeCore.GameModes.Factory;

public sealed class FactoryGame : GameMode<FactoryGame>, IGameMode, IDisposable
{
    public override Random? Rng { get; set; }

    public FactorySettings Settings { get; set; }

    public FactoryWorld? World { get; set; }
    /// <summary>
    /// The Map of tiles used for rendering
    /// </summary>
    public IntPtr TileMap { get; set; }

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
        World = new FactoryWorld(Settings.WorldWidth, Settings.WorldHeight, FactoryTerrain.Grass, Settings.InitialZoom, Settings.WorldBudget);
        ActorManager = new FactoryActorManager(Settings.ActorBudget);
        Rng = new Random(Settings.Seed);
    }

    public override void Init()
    {
        ChangeState(new StateFactoryMenu());
    }

    public override void Shutdown()
    {
        World?.Destroy();
        if (TileMap != IntPtr.Zero)
        {
            Native.TileMap_Destroy(TileMap);
            TileMap = IntPtr.Zero;
        }
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
