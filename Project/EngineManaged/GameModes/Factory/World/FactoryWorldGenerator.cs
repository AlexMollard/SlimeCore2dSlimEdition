using SlimeCore.GameModes.Factory.World;
using System;

namespace SlimeCore.GameModes.Factory.World;

public class FactoryWorldGenerator
{
    private readonly Random _rng;
    private readonly int _seed;

    public FactoryWorldGenerator(int seed)
    {
        _seed = seed;
        _rng = new Random(seed);
    }

    public void Generate(FactoryWorld world)
    {
        var w = world.Width();
        var h = world.Height();

        // 1. Fill with Grass
        world.SetAll(o =>
        {
            o.Type = FactoryTerrain.Grass;
            o.OreType = FactoryOre.None;
            o.IsMineable = false;
        });

        // 2. Generate Terrain Blobs
        GenerateBlobs(world, FactoryTerrain.Water, 20, 5, 15);
        GenerateBlobs(world, FactoryTerrain.Sand, 30, 4, 10);
        GenerateBlobs(world, FactoryTerrain.Stone, 40, 5, 20);

        // 3. Generate Ores
        // Iron on Stone or Grass
        GenerateOre(world, FactoryOre.Iron, 50, 3, 6, new[] { FactoryTerrain.Stone, FactoryTerrain.Grass });
        // Copper on Stone or Grass
        GenerateOre(world, FactoryOre.Copper, 40, 3, 6, new[] { FactoryTerrain.Stone, FactoryTerrain.Grass });
        // Coal on Stone
        GenerateOre(world, FactoryOre.Coal, 60, 2, 5, new[] { FactoryTerrain.Stone });
        // Gold on Stone (Rare)
        GenerateOre(world, FactoryOre.Gold, 15, 2, 4, new[] { FactoryTerrain.Stone });

        // 4. Set Mineable flags
        for (var x = 0; x < w; x++)
        {
            for (var y = 0; y < h; y++)
            {
                var tile = world[x, y];
                if (tile.OreType != FactoryOre.None)
                {
                    world.Set(x, y, o => o.IsMineable = true);
                }
                else if (tile.Type == FactoryTerrain.Stone || tile.Type == FactoryTerrain.Sand)
                {
                    world.Set(x, y, o => o.IsMineable = true);
                }
            }
        }
    }

    private void GenerateBlobs(FactoryWorld world, FactoryTerrain type, int count, int minRadius, int maxRadius)
    {
        var w = world.Width();
        var h = world.Height();

        for (var i = 0; i < count; i++)
        {
            var cx = _rng.Next(0, w);
            var cy = _rng.Next(0, h);
            var r = _rng.Next(minRadius, maxRadius);

            for (var x = cx - r; x <= cx + r; x++)
            {
                for (var y = cy - r; y <= cy + r; y++)
                {
                    if (x >= 0 && x < w && y >= 0 && y < h)
                    {
                        var dx = x - cx;
                        var dy = y - cy;
                        if (dx * dx + dy * dy <= r * r)
                        {
                            // Simple noise to make edges rough
                            if (_rng.NextDouble() > 0.2)
                            {
                                world.Set(x, y, o => o.Type = type);
                            }
                        }
                    }
                }
            }
        }
    }

    private void GenerateOre(FactoryWorld world, FactoryOre ore, int count, int minRadius, int maxRadius, FactoryTerrain[] allowedTerrains)
    {
        var w = world.Width();
        var h = world.Height();

        for (var i = 0; i < count; i++)
        {
            var cx = _rng.Next(0, w);
            var cy = _rng.Next(0, h);
            var r = _rng.Next(minRadius, maxRadius);

            for (var x = cx - r; x <= cx + r; x++)
            {
                for (var y = cy - r; y <= cy + r; y++)
                {
                    if (x >= 0 && x < w && y >= 0 && y < h)
                    {
                        var dx = x - cx;
                        var dy = y - cy;
                        if (dx * dx + dy * dy <= r * r)
                        {
                            var tile = world[x, y];
                            var allowed = false;
                            foreach (var t in allowedTerrains)
                            {
                                if (tile.Type == t)
                                {
                                    allowed = true;
                                    break;
                                }
                            }

                            if (allowed && _rng.NextDouble() > 0.3)
                            {
                                world.Set(x, y, o => o.OreType = ore);
                            }
                        }
                    }
                }
            }
        }
    }
}
