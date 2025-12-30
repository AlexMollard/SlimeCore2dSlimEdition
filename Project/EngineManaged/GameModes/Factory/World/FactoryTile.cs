using EngineManaged.Numeric;
using SlimeCore.Source.World.Grid;
using System;

namespace SlimeCore.GameModes.Factory.World;

public enum FactoryTerrain
{
    Grass,
    Concrete,
    Water,
    Sand,
    Stone
}

public enum FactoryOre
{
    None,
    Iron,
    Copper,
    Coal,
    Gold
}

public enum FactoryStructure
{
    None,
    ConveyorBelt,
    Miner,
    Storage
}

public sealed class FactoryTileOptions : TileOptions<FactoryTerrain>
{
    public FactoryOre OreType { get; set; } = FactoryOre.None;
    public bool IsMineable { get; set; }
    public FactoryStructure Structure { get; set; } = FactoryStructure.None;
    public int Tier { get; set; } = 1;
    public Direction Direction { get; set; } = Direction.North;
    public int Bitmask { get; set; }
}

public class FactoryTile : Tile<FactoryTerrain, FactoryTileOptions>
{
    public FactoryOre OreType { get; set; } = FactoryOre.None;
    public bool IsMineable { get; set; }
    public FactoryStructure Structure { get; set; } = FactoryStructure.None;
    public int Tier { get; set; } = 1;
    public Direction Direction { get; set; } = Direction.North;
    public int Bitmask { get; set; }

    public FactoryTile()
    {
    }

    public override void ApplyOptions(Action<FactoryTileOptions> configure)
    {
        var opts = new FactoryTileOptions
        {
            Type = Type,
            OreType = OreType,
            IsMineable = IsMineable,
            Structure = Structure,
            Tier = Tier,
            Direction = Direction,
            Bitmask = Bitmask
        };

        configure(opts);

        Type = opts.Type;
        OreType = opts.OreType;
        IsMineable = opts.IsMineable;
        Structure = opts.Structure;
        Tier = opts.Tier;
        Direction = opts.Direction;
        Bitmask = opts.Bitmask;
    }

    public override Vec3 GetPalette(params object[] extraArgs)
    {
        // Base color based on terrain
        var col = Type switch
        {
            FactoryTerrain.Grass => new Vec3(0.35f, 0.75f, 0.35f),
            FactoryTerrain.Concrete => new Vec3(0.6f, 0.6f, 0.65f),
            FactoryTerrain.Water => new Vec3(0.25f, 0.5f, 0.9f),
            FactoryTerrain.Sand => new Vec3(0.92f, 0.85f, 0.65f),
            FactoryTerrain.Stone => new Vec3(0.55f, 0.55f, 0.55f),
            _ => new Vec3(1, 0, 1)
        };

        // Tint for ore
        if (OreType != FactoryOre.None)
        {
            var oreCol = OreType switch
            {
                FactoryOre.Iron => new Vec3(0.8f, 0.4f, 0.4f),
                FactoryOre.Copper => new Vec3(0.8f, 0.5f, 0.2f),
                FactoryOre.Coal => new Vec3(0.2f, 0.2f, 0.2f),
                FactoryOre.Gold => new Vec3(1.0f, 0.85f, 0.2f),
                _ => new Vec3(1, 1, 1)
            };
            // Blend ore color on top (simple lerp for now)
            col = Vec3.Lerp(col, oreCol, 0.6f);
        }

        // Structure color override (temporary visualization)
        if (Structure == FactoryStructure.ConveyorBelt)
        {
            col = new Vec3(0.3f, 0.3f, 0.3f); // Dark grey for conveyor
            
            // Debug visualization for direction
            col += Direction switch
            {
                Direction.North => new Vec3(0.1f, 0, 0),
                Direction.East => new Vec3(0, 0.1f, 0),
                Direction.South => new Vec3(0, 0, 0.1f),
                Direction.West => new Vec3(0.1f, 0.1f, 0),
                _ => Vec3.Zero
            };
        }
        else if (Structure == FactoryStructure.Miner)
        {
            col = new Vec3(0.6f, 0.2f, 0.6f); // Purple for Miner
        }
        else if (Structure == FactoryStructure.Storage)
        {
            col = new Vec3(0.6f, 0.4f, 0.2f); // Brown for Storage
        }
        else
        {
            // Debug visualization for terrain bitmask (edges are darker)
            if (Bitmask != 15) // 15 = 1111 (all neighbors same)
            {
                col *= 0.9f;
            }
        }

        return col;
    }

    public bool IsBlocked()
    {
        if (Type == FactoryTerrain.Concrete)
        {
            return true;
        }
        if (Structure != FactoryStructure.None)
        {
            return true;
        }
        return false;
    }

    public bool IsLiquid()
    {
        if (Type == FactoryTerrain.Water)
        {
            return true;
        }
        return false;
    }
}
