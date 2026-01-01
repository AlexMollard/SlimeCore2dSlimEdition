using SlimeCore.Source.World.Actors;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.GameModes.Factory.Actors;

public enum FactoryActors
{
    Player = 0,
    DroppedItem = 1, // High priority to ensure smooth movement
    Animals = 2,
    Plants = 3,
}

public class FactoryActorManager : ActorManager<FactoryActors, FactoryGame>
{
    public FactoryActorManager(int actBudget, params Action<FactoryGame, float>[] priorityActions) : base(actBudget, priorityActions)
    {
    }
}
