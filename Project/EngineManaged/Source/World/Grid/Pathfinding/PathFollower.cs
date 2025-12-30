using EngineManaged.Numeric;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.Source.World.Grid.Pathfinding;
public class PathFollower<TPlanner>
where TPlanner : IPathPlanner
{
    private List<Vec2i>? _path;
    private int _index;
    private Vec2i _lastGoal;


    public Vec2 Update(
        Vec2 position,
        Vec2i goal,
        IWorldGrid world)
    {
        if (_path == null || _index >= _path.Count || goal != _lastGoal)
        {
            if (!TPlanner.TryFindPath(position.ToVec2Int(), goal, world, out _path))
                return Vec2.Zero;

            _index = 0;
            _lastGoal = goal;
        }

        var next = _path[_index];
        var nextCenter = next.ToVec2() + new Vec2(0.5f, 0.5f);

        if ((nextCenter - position).LengthSquared() < 0.05f)
            _index++;

        if (_index >= _path.Count)
            return Vec2.Zero;

        next = _path[_index];
        nextCenter = next.ToVec2() + new Vec2(0.5f, 0.5f);

        return (nextCenter - position).Normalized();
    }
}
