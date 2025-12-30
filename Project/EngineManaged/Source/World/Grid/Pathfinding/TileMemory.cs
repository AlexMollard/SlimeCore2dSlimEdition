using EngineManaged.Numeric;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SlimeCore.Source.World.Grid.Pathfinding;

public sealed class TileMemory
{
    private readonly Dictionary<Vec2i, float> _memory = new();

    public void Remember(Vec2i tile, float ttl = 2.5f)
        => _memory[tile] = ttl;

    public void Tick(float dt)
    {
        var keys = _memory.Keys.ToArray();
        foreach (var k in keys)
        {
            _memory[k] -= dt;
            if (_memory[k] <= 0)
            {
                _memory.Remove(k);
            }
        }
    }

    public bool IsAvoided(Vec2i tile) => _memory.ContainsKey(tile);
}

/// <summary>
/// "I was hurt there. I remember that place. I avoid it."
/// Moves away from tiles it has remembered as blocked/dangerous.
/// </summary>
public sealed class TileMemorySteering
{
    private readonly TileMemory _memory = new();

    public Vec2 ChooseDirection(
        Vec2 pos,
        Vec2 desired,
        IWorldGrid world,
        float dt)
    {
        _memory.Tick(dt);

        Span<float> angles = stackalloc float[] { 0f, 0.35f, -0.35f, 0.7f, -0.7f };

        foreach (float a in angles)
        {
            var dir = Vec2.Rotate(desired, a).Normalized();
            var tile = (pos + dir * 0.75f).ToVec2Int();

            if (world.IsBlocked(tile))
            {
                _memory.Remember(tile);
                continue;
            }

            if (_memory.IsAvoided(tile))
            {
                continue;
            }

            return dir;
        }

        return Vec2.Zero;
    }
}
