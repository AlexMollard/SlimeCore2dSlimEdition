using EngineManaged.Numeric;
using SlimeCore.Source.World.Grid;
using System;

namespace SlimeCore.GameModes.Factory.World;

public enum FactoryTerrain
{
    Grass,
    Dirt,
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

public class FactoryTile : Tile<FactoryGame, FactoryTerrain, FactoryTileOptions>
{
    public FactoryOre OreType { get; set; } = FactoryOre.None;
    public bool IsMineable { get; set; }
    public FactoryStructure Structure { get; set; } = FactoryStructure.None;
    public int Tier { get; set; } = 1;
    public Direction Direction { get; set; } = Direction.North;
    public int Bitmask { get; set; }
    public int Progress { get; set; }
    /// <summary>
    /// For tile rendering optimization, has this tile been rendered at least once? Does it need a re-render?
    /// </summary>
    public bool Rendered { get; private set; }

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
        if (Type != opts.Type)
        {
            Rendered = false;
            Type = opts.Type;
        }

        if (Structure != opts.Structure)
        {
            Rendered = false;
            Structure = opts.Structure;
        }

        if (Structure == FactoryStructure.ConveyorBelt && Direction != opts.Direction)
        {
            Rendered = false;
            Direction = opts.Direction;
        }

        OreType = opts.OreType;
        IsMineable = opts.IsMineable;
        Structure = opts.Structure;
        Tier = opts.Tier;
        Bitmask = opts.Bitmask;
    }

    public override Vec3 GetPalette(params object[] extraArgs)
    {
        // Base color based on terrain
        var col = Type switch
        {
            FactoryTerrain.Grass => new Vec3(0.35f, 0.75f, 0.35f),
            FactoryTerrain.Dirt => new Vec3(0.55f, 0.188f, 0.83f),
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

    public override void Tick(FactoryGame mode, float deltaTime)
    {
        if (Type == FactoryTerrain.Dirt)
        {
            if (Progress == 100)
            {
                Type = FactoryTerrain.Grass;
            }
        }
    }

    public override bool TakeAction(FactoryGame mode, float deltaTime)
    {
        if (!Rendered)
        {
            UpdateTile(mode);
        }
        return false;
    }

    public void UpdateTile(FactoryGame game)
    {
        if (game.TileMap == IntPtr.Zero || game.World == null) return;

        // Layer 0: Terrain
        Native.TileMap_SetTile(game.TileMap, PositionX, PositionY, 0, FactoryResources.GetTerrainTexture(Type), 1, 1, 1, 1, 0);

        // Layer 1: Ore
        var oreTex = FactoryResources.GetOreTexture(OreType);
        if (oreTex != IntPtr.Zero)
            Native.TileMap_SetTile(game.TileMap, PositionX, PositionY, 1, oreTex, 1, 1, 1, 1, 0);
        else
            Native.TileMap_SetTile(game.TileMap, PositionX, PositionY, 1, IntPtr.Zero, 0, 0, 0, 0, 0);

        // Layer 2: Structure
        var structTex = FactoryResources.GetStructureTexture(Structure, Tier);
        var rotation = 0.0f;

        if (Structure == FactoryStructure.ConveyorBelt)
        {
            // Use ConveyorSystem
            game.ConveyorSystem.PlaceConveyor(PositionX, PositionY, Tier, Direction);
            game.BuildingSystem.RemoveBuilding(PositionX, PositionY); // Ensure no building

            // Don't render in TileMap
            Native.TileMap_SetTile(game.TileMap, PositionX, PositionY, 2, IntPtr.Zero, 0, 0, 0, 0, 0);
        }
        else if (Structure == FactoryStructure.Miner || Structure == FactoryStructure.Storage)
        {
            // Use BuildingSystem
            game.BuildingSystem.PlaceBuilding(PositionX, PositionY, Structure, Direction, Tier);
            game.ConveyorSystem.RemoveConveyor(PositionX, PositionY); // Ensure no conveyor

            if (structTex != IntPtr.Zero)
            {
                // Use the texture
                Native.TileMap_SetTile(game.TileMap, PositionX, PositionY, 2, structTex, 1, 1, 1, 1, rotation);
            }
            else
            {
                // Fallback: Use a generic square (maybe the conveyor texture?) but we want color.
                var baseTex = FactoryResources.GetTerrainTexture(FactoryTerrain.Concrete);

                if (Structure == FactoryStructure.Miner)
                {
                    Native.TileMap_SetTile(game.TileMap, PositionX, PositionY, 2, baseTex, 0.6f, 0.2f, 0.6f, 1.0f, 0);
                }
                else
                    Native.TileMap_SetTile(game.TileMap, PositionX, PositionY, 2, baseTex, 0.6f, 0.4f, 0.2f, 1.0f, 0);
            }
        }
        else
        {
            // Remove from systems
            game.ConveyorSystem.RemoveConveyor(PositionX, PositionY);
            game.BuildingSystem.RemoveBuilding(PositionX, PositionY);

            if (structTex != IntPtr.Zero)
                Native.TileMap_SetTile(game.TileMap, PositionX, PositionY, 2, structTex, 1, 1, 1, 1, rotation);
            else
                Native.TileMap_SetTile(game.TileMap, PositionX, PositionY, 2, IntPtr.Zero, 0, 0, 0, 0, 0);
        }

        game.World.ShouldRender = true;
        Rendered = true;
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
