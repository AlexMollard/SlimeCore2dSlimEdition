using SlimeCore.Source.World.Actors;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.GameModes.Factory.Actors;

public enum FactoryActors
{
    Player = 0,
    OnScreenEntity = 1, //Reserved for entities currently on screen/Always on screen
    Animals = 2,
    DroppedItem = 3,
    Plants = 4,
}

public class FactoryActorManager : ActorManager<FactoryActors, FactoryGame>
{
    public FactoryActorManager(int actBudget, params Action<FactoryGame, float>[] priorityActions) : base(actBudget, priorityActions)
    {
    }
}
