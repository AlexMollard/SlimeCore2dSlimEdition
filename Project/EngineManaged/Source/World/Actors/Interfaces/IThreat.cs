using EngineManaged.Numeric;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.Source.World.Actors.Interfaces;

public interface IThreat
{
    Vec2 Position { get; }
    float Radius { get; }
}