using System;

namespace SlimeCore.GameModes.Factory.World;

public class FactoryWorldGenerator
{
    private readonly int _seed;
    private readonly Random _rng;
    private int[] _permutation = null!;

    public FactoryWorldGenerator(int seed)
    {
        _seed = seed;
        _rng = new Random(seed);
        InitializeNoise();
    }

    private void InitializeNoise()
    {
        _permutation = new int[512];
        int[] p = new int[256];
        for (int i = 0; i < 256; i++) p[i] = i;

        // Shuffle
        for (int i = 0; i < 256; i++)
        {
            int swapIdx = _rng.Next(256);
            (p[swapIdx], p[i]) = (p[i], p[swapIdx]);
        }

        for (int i = 0; i < 256; i++)
        {
            _permutation[i] = p[i];
            _permutation[i + 256] = p[i];
        }
    }

    private float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);

    private float Lerp(float t, float a, float b) => a + t * (b - a);

    private float Grad(int hash, float x, float y)
    {
        int h = hash & 15;
        float u = h < 8 ? x : y;
        float v = h < 4 ? y : h == 12 || h == 14 ? x : 0;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    private float Noise(float x, float y)
    {
        int X = (int)Math.Floor(x) & 255;
        int Y = (int)Math.Floor(y) & 255;

        x -= (float)Math.Floor(x);
        y -= (float)Math.Floor(y);

        float u = Fade(x);
        float v = Fade(y);

        int A = _permutation[X] + Y;
        int AA = _permutation[A];
        int AB = _permutation[A + 1];
        int B = _permutation[X + 1] + Y;
        int BA = _permutation[B];
        int BB = _permutation[B + 1];

        return Lerp(v, Lerp(u, Grad(_permutation[AA], x, y),
                               Grad(_permutation[BA], x - 1, y)),
                       Lerp(u, Grad(_permutation[AB], x, y - 1),
                               Grad(_permutation[BB], x - 1, y - 1)));
    }

    private float OctaveNoise(float x, float y, int octaves, float persistence)
    {
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float maxValue = 0;

        for (int i = 0; i < octaves; i++)
        {
            total += Noise(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= 2;
        }

        return total / maxValue;
    }

    public void Generate(FactoryWorld world)
    {
        int w = world.Width();
        int h = world.Height();

        // Scale factors
        float terrainScale = 0.02f; // Slightly larger features
        float oreScale = 0.08f;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                // 1. Terrain Generation
                // n represents elevation: -1 to 1
                float n = OctaveNoise(x * terrainScale, y * terrainScale, 4, 0.5f);
                
                var type = FactoryTerrain.Grass;
                
                // Thresholds for terrain
                // Deep water: < -0.3
                // Water: -0.3 to -0.2
                // Sand: -0.2 to -0.1
                // Grass: -0.1 to 0.35
                // Stone (Mountain): > 0.35
                
                if (n < -0.2f) type = FactoryTerrain.Water;
                else if (n < -0.1f) type = FactoryTerrain.Sand;
                else if (n > 0.35f) type = FactoryTerrain.Stone;

                var ore = FactoryOre.None;

                // 2. Ore Generation (only on land)
                if (type != FactoryTerrain.Water)
                {
                    // Ore noise maps
                    float ironNoise = OctaveNoise((x + 1234) * oreScale, (y + 1234) * oreScale, 2, 0.5f);
                    float copperNoise = OctaveNoise((x + 2345) * oreScale, (y + 2345) * oreScale, 2, 0.5f);
                    float coalNoise = OctaveNoise((x + 3456) * oreScale, (y + 3456) * oreScale, 2, 0.5f);
                    float goldNoise = OctaveNoise((x + 4567) * oreScale, (y + 4567) * oreScale, 2, 0.5f);

                    // Base thresholds (higher = rarer)
                    float ironThresh = 0.65f;
                    float copperThresh = 0.65f;
                    float coalThresh = 0.65f;
                    float goldThresh = 0.75f;

                    // Elevation Influence:
                    // Ores are much more common at higher elevations (near and inside mountains)
                    // n ranges roughly from -0.1 (coast) to 0.8 (peak) on land
                    
                    if (n > 0.1f) // Starting at "foothills"
                    {
                        // The higher we go, the more likely ores are
                        float elevationBonus = (n - 0.1f) * 0.8f; 
                        
                        ironThresh -= elevationBonus;
                        copperThresh -= elevationBonus;
                        coalThresh -= elevationBonus;
                        goldThresh -= elevationBonus;
                    }

                    // Biome specific tweaks
                    if (type == FactoryTerrain.Stone)
                    {
                        // Mountains are rich in Coal and Gold
                        coalThresh -= 0.15f;
                        goldThresh -= 0.15f;
                    }
                    else if (type == FactoryTerrain.Grass && n > 0.2f)
                    {
                        // High grass (near mountains) is rich in Iron and Copper
                        ironThresh -= 0.1f;
                        copperThresh -= 0.1f;
                    }

                    // Determine ore (Priority system)
                    if (goldNoise > goldThresh) ore = FactoryOre.Gold;
                    else if (coalNoise > coalThresh) ore = FactoryOre.Coal;
                    else if (ironNoise > ironThresh) ore = FactoryOre.Iron;
                    else if (copperNoise > copperThresh) ore = FactoryOre.Copper;
                }

                // 3. Set Mineable flags
                bool isMineable = (ore != FactoryOre.None) || (type == FactoryTerrain.Stone) || (type == FactoryTerrain.Sand);

                world.Set(x, y, o =>
                {
                    o.Type = type;
                    o.OreType = ore;
                    o.IsMineable = isMineable;
                });
            }
        }
    }
}
