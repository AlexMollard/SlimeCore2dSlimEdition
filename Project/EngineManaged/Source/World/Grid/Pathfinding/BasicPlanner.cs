using EngineManaged.Numeric;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.Source.World.Grid.Pathfinding;

public class BasicPlanner : IPathPlanner
{
    public static bool TryFindPath(
        Vec2i from,
        Vec2i to,
        IWorldGrid world,
        out List<Vec2i> path)
    {
        path = new List<Vec2i>();
        var current = from;

        path.Add(current);

        while (current != to)
        {
            int dx = Math.Sign(to.X - current.X);
            int dy = Math.Sign(to.Y - current.Y);

            var next = new Vec2i(
                current.X + dx,
                current.Y + dy
            );
            bool blocked = world.IsBlocked(next);

            if (!world.InBounds(next) || blocked)
                return false;

            current = next;
            path.Add(current);
        }

        return true;
    }
}

