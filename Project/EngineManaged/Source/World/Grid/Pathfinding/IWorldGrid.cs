using EngineManaged.Numeric;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.Source.World.Grid.Pathfinding;

public interface IWorldGrid
{
    public bool IsBlocked(Vec2i tile);
    public bool IsLiquid(Vec2i tile);
    public bool InBounds(Vec2i tile);
}