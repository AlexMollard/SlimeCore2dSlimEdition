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

    public FactoryActorManager? ActorManager { get; set; }

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
        World = new FactoryWorld(Settings.WorldWidth, Settings.WorldHeight, FactoryTerrain.Grass, Settings.InitialZoom);
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
        World?.Destroy();
    }
}
