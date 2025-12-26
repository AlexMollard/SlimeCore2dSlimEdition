using GameModes.Dude;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.Source.Core;
/// <summary>
/// Represents a state within a game mode
/// </summary>
/// <typeparam name="TMode">The Game mode</typeparam>
public interface IGameState<TMode>
    where TMode : IGameMode
{
    void Enter(TMode game);
    void Update(TMode game, float dt);
    void Exit(TMode game);
}
