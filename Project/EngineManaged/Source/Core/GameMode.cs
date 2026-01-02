using EngineManaged.Numeric;
using System;

namespace SlimeCore.Source.Core;
/// <summary>
/// Represents a game mode that can have multiple states
/// </summary>
/// <typeparam name="TGameMode"></typeparam>
public abstract class GameMode<TGameMode> : IGameMode
    where TGameMode : GameMode<TGameMode>
{
    public Vec2 Camera;
    /// <summary>
    /// Is this position within the current view?
    /// </summary>
    public abstract bool InView(Vec2 position);

    protected IGameState<TGameMode>? _currentState;

    public void ChangeState(IGameState<TGameMode> newState)
    {
        if (_currentState != null)
        {
            _currentState.Exit((TGameMode)this);
        }

        _currentState = newState;

        if (_currentState != null)
        {
            _currentState.Enter((TGameMode)this);
        }
    }

    public abstract void Init();

    public abstract void Update(float dt);

    public virtual void Draw()
    {
        if (_currentState != null)
        {
            _currentState.Draw((TGameMode)this);
        }
    }

    public abstract void Shutdown();

    public abstract Random? Rng { get; set; }
}

