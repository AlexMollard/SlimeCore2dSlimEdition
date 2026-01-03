using EngineManaged.Numeric;
using SlimeCore.Source.Common;
using SlimeCore.Source.World.Actors;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.Source.World.Grid.Pathfinding;

public sealed class TileAStarPlanner : IPathPlanner
{
    public static bool TryFindPath(
        Vec2i from,
        Vec2i to,
        IWorldGrid world,
        out List<Vec2i> path)
    {
        path = new List<Vec2i>();

        var open = new PriorityQueue<Vec2i, float>();
        var cameFrom = new Dictionary<Vec2i, Vec2i>();
        var gScore = new Dictionary<Vec2i, float> { [from] = 0f };
        var closed = new HashSet<Vec2i>();

        open.Enqueue(from, Heuristic(from, to));

        while (open.Count > 0 && path.Count < 512)
        {
            var current = open.Dequeue();
            if (!closed.Add(current))
                continue;

            if (current == to)
            {
                ReconstructPath(cameFrom, current, path);
                return true;
            }

            foreach (var next in Neighbours(current))
            {
                if (!world.InBounds(next) || world.IsBlocked(next))
                    continue;

                float tentative = gScore[current] + 1f;

                if (!gScore.TryGetValue(next, out float score) || tentative < score)
                {
                    cameFrom[next] = current;
                    gScore[next] = tentative;
                    open.Enqueue(next, tentative + Heuristic(next, to));
                }
            }
        }
        return false;
    }

    private static float Heuristic(Vec2i a, Vec2i b)
        => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y); // Manhattan

    private static IEnumerable<Vec2i> Neighbours(Vec2i p)
    {
        yield return new Vec2i(p.X + 1, p.Y);
        yield return new Vec2i(p.X - 1, p.Y);
        yield return new Vec2i(p.X, p.Y + 1);
        yield return new Vec2i(p.X, p.Y - 1);
    }

    private static void ReconstructPath(
        Dictionary<Vec2i, Vec2i> cameFrom,
        Vec2i current,
        List<Vec2i> path)
    {
        path.Add(current);
        while (cameFrom.TryGetValue(current, out var prev))
        {
            current = prev;
            path.Add(current);
        }
        path.Reverse();
    }

}

