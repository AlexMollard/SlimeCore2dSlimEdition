using SlimeCore.Source.World.Actors;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.GameModes.Factory.Actors;

public class Sheep : Actor<FactoryActors, FactoryGame>
{
    public override FactoryActors Kind { get; }
    public override bool TakeAction(FactoryGame mode, float deltaTime)
    {
        throw new NotImplementedException();
    }

    public override void Destroy()
    {
        throw new NotImplementedException();
    }

    protected override float ActionInterval { get; }
}
