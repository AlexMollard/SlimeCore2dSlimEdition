using EngineManaged.Numeric;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.Source.World.Grid.Pathfinding;

public interface IPathPlanner
{
    static abstract bool TryFindPath(Vec2i from, Vec2i to, IWorldGrid world, out List<Vec2i> path);
}
