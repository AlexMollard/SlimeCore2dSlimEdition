using SlimeCore.Source.World.Actors;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.GameModes.Factory.Actors;

public enum FactoryActors
{
    Player = 0,
    Animals = 1,
    Plants = 2,
}

public class FactoryActorManager : ActorManager<FactoryActors, FactoryGame>
{
    public FactoryActorManager(int actBudget, params Action<FactoryGame, float>[] priorityActions) : base(actBudget, priorityActions)
    {
    }
}
