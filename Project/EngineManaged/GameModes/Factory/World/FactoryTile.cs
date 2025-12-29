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
    ConveyorBelt
}

public sealed class FactoryTileOptions : TileOptions<FactoryTerrain>
{
    public FactoryOre OreType { get; set; } = FactoryOre.None;
    public bool IsMineable { get; set; }
    public FactoryStructure Structure { get; set; } = FactoryStructure.None;
}

public class FactoryTile : Tile<FactoryTerrain, FactoryTileOptions>
{
    public FactoryOre OreType { get; set; } = FactoryOre.None;
    public bool IsMineable { get; set; }
    public FactoryStructure Structure { get; set; } = FactoryStructure.None;

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
            Structure = Structure
        };

        configure(opts);

        Type = opts.Type;
        OreType = opts.OreType;
        IsMineable = opts.IsMineable;
        Structure = opts.Structure;
    }

    public override Vec3 GetPalette(params object[] extraArgs)
    {
        // Base color based on terrain
        var col = Type switch
        {
            FactoryTerrain.Grass => new Vec3(0.2f, 0.8f, 0.2f),
            FactoryTerrain.Concrete => new Vec3(0.5f, 0.5f, 0.5f),
            FactoryTerrain.Water => new Vec3(0.2f, 0.2f, 0.9f),
            FactoryTerrain.Sand => new Vec3(0.9f, 0.8f, 0.5f),
            FactoryTerrain.Stone => new Vec3(0.4f, 0.4f, 0.4f),
            _ => new Vec3(1, 0, 1)
        };

        // Tint for ore
        if (OreType != FactoryOre.None)
        {
            var oreCol = OreType switch
            {
                FactoryOre.Iron => new Vec3(0.8f, 0.4f, 0.4f),
                FactoryOre.Copper => new Vec3(0.8f, 0.5f, 0.2f),
                FactoryOre.Coal => new Vec3(0.1f, 0.1f, 0.1f),
                FactoryOre.Gold => new Vec3(1.0f, 0.8f, 0.0f),
                _ => new Vec3(1, 1, 1)
            };
            // Blend ore color on top (simple lerp for now)
            col = Vec3.Lerp(col, oreCol, 0.5f);
        }

        // Structure color override (temporary visualization)
        if (Structure == FactoryStructure.ConveyorBelt)
        {
            col = new Vec3(0.3f, 0.3f, 0.3f); // Dark grey for conveyor
        }

        return col;
    }
}
