using SlimeCore.Core.Grid;
using System;
using System.Collections.Generic;

namespace SlimeCore.Core.World;

internal class WorldGenerator
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

    public void Generate(GridSystem<Terrain> world)
    {
        int w = world.Width();
        int h = world.Height();

        // 1. INITIALIZE CANVAS
        world.SetAll(o => {
            o.Blocked = false;
            o.Type = Terrain.Grass;
        });

        // 2. DEFINE HIGHWAYS
        List<int> highwayX = new List<int>();
        List<int> highwayY = new List<int>();
        foreach(float p in _highwayPercent)
        {
            highwayX.Add((int)(w * p));
            highwayY.Add((int)(h * p));
        }

        // 3. GENERATE CITY BLOCKS
        // We start slightly offset to avoid cutting off blocks at the wrap point
        int cx = STREET_WIDTH;
        while (cx < w - STREET_WIDTH)
        {
            if (IsOverlappingHighway(cx, highwayX)) 
            {
                cx += HIGHWAY_WIDTH;
                continue;
            }

            int blockW = _rng.Next(BLOCK_MIN, BLOCK_MAX);
            blockW = ClampBlockToHighway(cx, blockW, highwayX, w);

            int cy = STREET_WIDTH;
            while (cy < h - STREET_WIDTH)
            {
                if (IsOverlappingHighway(cy, highwayY))
                {
                    cy += HIGHWAY_WIDTH;
                    continue;
                }

                int blockH = _rng.Next(BLOCK_MIN, BLOCK_MAX);
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
        foreach(int h in highways)
            if (pos >= h - 2 && pos <= h + HIGHWAY_WIDTH + 2) return true;
        return false;
    }

    private int ClampBlockToHighway(int pos, int size, List<int> highways, int max)
    {
        int end = pos + size;
        if (end >= max - STREET_WIDTH) size = max - STREET_WIDTH - pos;
        foreach (int h in highways)
        {
            if (pos < h && end > h - STREET_WIDTH)
                size = (h - STREET_WIDTH) - pos;
        }
        return size;
    }

    private void ProcessPlaza(GridSystem<Terrain> world, int x, int y, int w, int h)
    {
        double roll = _rng.NextDouble();
        if (roll < 0.45)
        {
            FillRect(world, x, y, w, h, e =>
            {
                e.Type = Terrain.Rock;
                e.Blocked = true;
            });
        }
        else if (roll < 0.65)
        {
            FillRect(world, x, y, w, h, e =>
            {
                e.Type = Terrain.Mud;
                e.Blocked = false;
            });
        }
        else if (roll < 0.80)
        {
            FillRect(world, x, y, w, h, e =>
            {
                e.Type = Terrain.Ice;
                e.Blocked = false;
            });
        }
        else if (roll < 0.85)
        {
            FillRect(world, x + 1, y + 1, w - 2, h - 2, e =>
            {
                e.Type = Terrain.Lava;
                e.Blocked = true;
            });
        }
    }

    private void DrawHighways(GridSystem<Terrain> world, List<int> hx, List<int> hy)
    {
        int w = world.Width();
        int h = world.Height();
        foreach (int x in hx)
            for (int y = 0; y < h; y++)
            {
                world.Set(x, y, o =>
                {
                    o.Type = Terrain.Speed;
                    o.Blocked = false;
                });
                world.Set(x + 1, y, o =>
                {
                    o.Type = Terrain.Speed;
                    o.Blocked = false;
                });
            }
        foreach (int y in hy)
            for (int x = 0; x < w; x++)
            {
                world.Set(x, y, o =>
                {
                    o.Type = Terrain.Speed;
                    o.Blocked = false;
                });
                world.Set(x, y + 1, o =>
                {
                    o.Type = Terrain.Speed;
                    o.Blocked = false;
                });
            }
    }

    private void RemoveSingleTileWalls(GridSystem<Terrain> world)
    {
        int w = world.Width();
        int h = world.Height();
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (world[x, y].Blocked)
                {
                    int neighbors = 0;
                    // Wrap check for neighbors
                    if (world[(x + 1) % w, y].Blocked) neighbors++;
                    if (world[(x - 1 + w) % w, y].Blocked) neighbors++;
                    if (world[x, (y + 1) % h].Blocked) neighbors++;
                    if (world[x, (y - 1 + h) % h].Blocked) neighbors++;

                    if (neighbors < 1) 
                    {
                        world[x, y].Blocked = false;
                        world[x, y].Type = Terrain.Grass;
                    }
                }
            }
        }
    }

    private void FillRect(GridSystem<Terrain> world, int bx, int by, int bw, int bh, Action<TileOptions<Terrain>> configure)
    {
        for (int x = bx; x < bx + bw; x++)
            for (int y = by; y < by + bh; y++)
            {
                world.Set(x, y, configure);
            }
    }

    private void ClearCenter(GridSystem<Terrain> world)
    {
        int cw = world.Width() / 2;
        int ch = world.Height() / 2;
        int r = 10;
        for (int x = cw - r; x <= cw + r; x++)
            for (int y = ch - r; y <= ch + r; y++)
            {
                world.Set(x, y, o =>
                {
                    o.Blocked = false;
                    o.Type = Terrain.Grass;
                });
            }
    }

    private void PruneUnreachableAreas(GridSystem<Terrain> world)
    {
        int w = world.Width();
        int h = world.Height();
        bool[,] reachable = new bool[w, h];
        Queue<(int x, int y)> queue = new Queue<(int x, int y)>();
        
        int startX = w / 2;
        int startY = h / 2;
        
        queue.Enqueue((startX, startY));
        reachable[startX, startY] = true;

        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                // FLOOD FILL NOW WRAPS AROUND THE WORLD
                int nx = (cx + dx[i] + w) % w;
                int ny = (cy + dy[i] + h) % h;

                if (!reachable[nx, ny] && !world[nx, ny].Blocked)
                {
                    reachable[nx, ny] = true;
                    queue.Enqueue((nx, ny));
                }
            }
        }

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                if (!world[x, y].Blocked && !reachable[x, y])
                {
                    world[x, y].Blocked = true;
                    world[x, y].Type = Terrain.Rock;
                }
            }
    }
}