using EngineManaged.Numeric;
using GameModes.Dude;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.GameModes.Snake.Actors;

public class SnakeGameEvents
{

    // Called when an enemy dies. Passes the position of death.
    public Action<SnakeGame, Vec2>? OnEnemyKilled;

    public void Clear()
    {
        OnEnemyKilled = null;
    }
}
