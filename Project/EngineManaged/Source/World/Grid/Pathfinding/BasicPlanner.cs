using EngineManaged.Numeric;
using SlimeCore.Source.Common;
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
        var previousPositions = new HashSet<Vec2i>();
        var current = from;

        path.Add(current);


        while (current != to && path.Count < 1024)
        {
            var best = world.GetBestNeighbour(current, to, previousPositions);
            if (best == null)
            {
                return false;
            }
            current = best.Value;
            previousPositions.Add(current);
            path.Add(current);
        }
        //Logger.Info($"PathGenerated {path.Count}");
        return true;
    }
}

