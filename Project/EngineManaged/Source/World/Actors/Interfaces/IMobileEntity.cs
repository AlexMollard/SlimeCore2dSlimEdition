using EngineManaged.Numeric;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.Source.World.Actors.Interfaces;

public interface IMobileEntity
{
    Vec2 Position { get; set; }
    Vec2 Velocity { get; set; }
}
