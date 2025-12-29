using System;
using System.Collections.Generic;

namespace SlimeCore.GameModes.Snake.World;

public class WorldGenerator
{
    public int Seed { get; }
    private readonly Random _rng;

    private const int BLOCK_MIN = 8;
    private const int BLOCK_MAX = 15;
    private const int STREET_WIDTH = 3;
    private const int HIGHWAY_WIDTH = 4;

    private readonly float[] _highwayPercent = new float[] { 0.25f, 0.75f };

    public WorldGenerator(int seed)
    {
        Seed = seed;
        _rng = new Random(seed);
    }

    public void Generate(SnakeGrid world)
    {
        var w = world.Width();
        var h = world.Height();

        // 1. INITIALIZE CANVAS
        world.SetAll(o =>
        {
            o.Blocked = false;
            o.Type = SnakeTerrain.Grass;
        });

        // 2. DEFINE HIGHWAYS
        var highwayX = new List<int>();
        var highwayY = new List<int>();
        foreach (var p in _highwayPercent)
        {
            highwayX.Add((int)(w * p));
            highwayY.Add((int)(h * p));
        }

        // 3. GENERATE CITY BLOCKS
        // We start slightly offset to avoid cutting off blocks at the wrap point
        var cx = STREET_WIDTH;
        while (cx < w - STREET_WIDTH)
        {
            if (IsOverlappingHighway(cx, highwayX))
            {
                cx += HIGHWAY_WIDTH;
                continue;
            }

            var blockW = _rng.Next(BLOCK_MIN, BLOCK_MAX);
            blockW = ClampBlockToHighway(cx, blockW, highwayX, w);

            var cy = STREET_WIDTH;
            while (cy < h - STREET_WIDTH)
            {
                if (IsOverlappingHighway(cy, highwayY))
                {
                    cy += HIGHWAY_WIDTH;
                    continue;
                }

                var blockH = _rng.Next(BLOCK_MIN, BLOCK_MAX);
                blockH = ClampBlockToHighway(cy, blockH, highwayY, h);

                if (blockW > 4 && blockH > 4)
                {
                    ProcessPlaza(world, cx, cy, blockW, blockH);
                }

                cy += blockH + STREET_WIDTH;
            }
            cx += blockW + STREET_WIDTH;
        }

        // 4. DRAW HIGHWAYS
        DrawHighways(world, highwayX, highwayY);

        // 5. CLEANUP
        ClearCenter(world);
        RemoveSingleTileWalls(world);

        // Check connectivity with wrapping logic
        PruneUnreachableAreas(world);

        // REMOVED: ApplyEdgeWalls(world); 
        // The map is now infinite!
    }

    private bool IsOverlappingHighway(int pos, List<int> highways)
    {
        foreach (var h in highways)
            if (pos >= h - 2 && pos <= h + HIGHWAY_WIDTH + 2) return true;
        return false;
    }

    private int ClampBlockToHighway(int pos, int size, List<int> highways, int max)
    {
        var end = pos + size;
        if (end >= max - STREET_WIDTH) size = max - STREET_WIDTH - pos;
        foreach (var h in highways)
        {
            if (pos < h && end > h - STREET_WIDTH)
                size = (h - STREET_WIDTH) - pos;
        }
        return size;
    }

    private void ProcessPlaza(SnakeGrid world, int x, int y, int w, int h)
    {
        var roll = _rng.NextDouble();
        if (roll < 0.45)
        {
            FillRect(world, x, y, w, h, e =>
            {
                e.Type = SnakeTerrain.Rock;
                e.Blocked = true;
            });
        }
        else if (roll < 0.65)
        {
            FillRect(world, x, y, w, h, e =>
            {
                e.Type = SnakeTerrain.Mud;
                e.Blocked = false;
            });
        }
        else if (roll < 0.80)
        {
            FillRect(world, x, y, w, h, e =>
            {
                e.Type = SnakeTerrain.Ice;
                e.Blocked = false;
            });
        }
        else if (roll < 0.85)
        {
            FillRect(world, x + 1, y + 1, w - 2, h - 2, e =>
            {
                e.Type = SnakeTerrain.Lava;
                e.Blocked = true;
            });
        }
    }

    private void DrawHighways(SnakeGrid world, List<int> hx, List<int> hy)
    {
        var w = world.Width();
        var h = world.Height();
        foreach (var x in hx)
            for (var y = 0; y < h; y++)
            {
                world.Set(x, y, o =>
                {
                    o.Type = SnakeTerrain.Speed;
                    o.Blocked = false;
                });
                world.Set(x + 1, y, o =>
                {
                    o.Type = SnakeTerrain.Speed;
                    o.Blocked = false;
                });
            }
        foreach (var y in hy)
            for (var x = 0; x < w; x++)
            {
                world.Set(x, y, o =>
                {
                    o.Type = SnakeTerrain.Speed;
                    o.Blocked = false;
                });
                world.Set(x, y + 1, o =>
                {
                    o.Type = SnakeTerrain.Speed;
                    o.Blocked = false;
                });
            }
    }

    private void RemoveSingleTileWalls(SnakeGrid world)
    {
        var w = world.Width();
        var h = world.Height();
        for (var x = 0; x < w; x++)
        {
            for (var y = 0; y < h; y++)
            {
                if (world[x, y].Blocked)
                {
                    var neighbors = 0;
                    // Wrap check for neighbors
                    if (world[(x + 1) % w, y].Blocked) neighbors++;
                    if (world[(x - 1 + w) % w, y].Blocked) neighbors++;
                    if (world[x, (y + 1) % h].Blocked) neighbors++;
                    if (world[x, (y - 1 + h) % h].Blocked) neighbors++;

                    if (neighbors < 1)
                    {
                        world[x, y].Blocked = false;
                        world[x, y].Type = SnakeTerrain.Grass;
                    }
                }
            }
        }
    }

    private void FillRect(SnakeGrid world, int bx, int by, int bw, int bh, Action<SnakeTileOptions> configure)
    {
        for (var x = bx; x < bx + bw; x++)
            for (var y = by; y < by + bh; y++)
            {
                world.Set(x, y, configure);
            }
    }

    private void ClearCenter(SnakeGrid world)
    {
        var cw = world.Width() / 2;
        var ch = world.Height() / 2;
        var r = 10;
        for (var x = cw - r; x <= cw + r; x++)
            for (var y = ch - r; y <= ch + r; y++)
            {
                world.Set(x, y, o =>
                {
                    o.Blocked = false;
                    o.Type = SnakeTerrain.Grass;
                });
            }
    }

    private void PruneUnreachableAreas(SnakeGrid world)
    {
        var w = world.Width();
        var h = world.Height();
        var reachable = new bool[w][];
        for (var i = 0; i < w; i++)
        {
            reachable[i] = new bool[h];
        }

        var queue = new Queue<(int x, int y)>();

        var startX = w / 2;
        var startY = h / 2;

        queue.Enqueue((startX, startY));
        reachable[startX][startY] = true;

        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            for (var i = 0; i < 4; i++)
            {
                // FLOOD FILL NOW WRAPS AROUND THE WORLD
                var nx = (cx + dx[i] + w) % w;
                var ny = (cy + dy[i] + h) % h;

                if (!reachable[nx][ny] && !world[nx, ny].Blocked)
                {
                    reachable[nx][ny] = true;
                    queue.Enqueue((nx, ny));
                }
            }
        }

        for (var x = 0; x < w; x++)
            for (var y = 0; y < h; y++)
            {
                if (!world[x, y].Blocked && !reachable[x][y])
                {
                    world[x, y].Blocked = true;
                    world[x, y].Type = SnakeTerrain.Rock;
                }
            }
    }
}