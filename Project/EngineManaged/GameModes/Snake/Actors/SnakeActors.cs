using SlimeCore.Source.World.Actors;
using System;

namespace SlimeCore.GameModes.Snake.Actors;

public enum SnakeActors : int
{
    Snake = 0,
    SnakeHunter = 1,
    SnakeFood = 2,
}

public class SnakeActorManager : ActorManager<SnakeActors, SnakeGame>
{
    public SnakeActorManager(int actBudget, params Action<SnakeGame, float>[] additionalActions) : base(actBudget, additionalActions)
    {
    }
}

