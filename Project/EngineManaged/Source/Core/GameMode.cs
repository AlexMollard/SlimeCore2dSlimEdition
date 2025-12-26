using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.Source.Core;
/// <summary>
/// Represents a game mode that can have multiple states
/// </summary>
/// <typeparam name="TGameMode"></typeparam>
public abstract class GameMode<TGameMode> : IGameMode
    where TGameMode : GameMode<TGameMode>
{
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

    public abstract void Shutdown();

    public abstract Random? Rng { get; set; }
}

