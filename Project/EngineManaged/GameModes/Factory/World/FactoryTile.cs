using EngineManaged.Numeric;
using SlimeCore.GameModes.Factory.Actors;
using SlimeCore.Source.World.Grid;
using System;
using System.Collections.Generic;

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
    Storage,
    FarmPlot,
    Wall,
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
    public int ConveyorBitmask { get; set; }
    public int Progress { get; set; }
    
    public List<DroppedItem> Items { get; } = new();

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

        if ((Structure == FactoryStructure.ConveyorBelt || Structure == FactoryStructure.FarmPlot || Structure == FactoryStructure.Wall) && Direction != opts.Direction)
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

            Progress += 10;
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

    public static void GetTerrainUVs(int bitmask, out float u0, out float v0, out float u1, out float v1)
    {
        int x = 1;
        int y = 1;

        switch (bitmask)
        {
            case 0: x = 1; y = 1; break; // None -> Center
            case 1: x = 1; y = 2; break; // N -> Bottom Edge
            case 2: x = 0; y = 1; break; // E -> Left Edge
            case 3: x = 0; y = 2; break; // N|E -> BL Corner
            case 4: x = 1; y = 0; break; // S -> Top Edge
            case 5: x = 1; y = 1; break; // N|S -> Center (Vertical)
            case 6: x = 0; y = 0; break; // S|E -> TL Corner
            case 7: x = 0; y = 1; break; // N|S|E -> Left Edge
            case 8: x = 2; y = 1; break; // W -> Right Edge
            case 9: x = 2; y = 2; break; // N|W -> BR Corner
            case 10: x = 1; y = 1; break; // W|E -> Center (Horizontal)
            case 11: x = 1; y = 2; break; // N|W|E -> Bottom Edge
            case 12: x = 2; y = 0; break; // S|W -> TR Corner
            case 13: x = 2; y = 1; break; // N|S|W -> Right Edge
            case 14: x = 1; y = 0; break; // S|W|E -> Top Edge
            case 15: x = 1; y = 1; break; // All -> Center
        }

        float size = 1.0f / 3.0f;
        u0 = x * size;
        v0 = y * size;
        u1 = u0 + size;
        v1 = v0 + size;
    }

    public void UpdateTile(FactoryGame game)
    {
        if (game.TileMap == IntPtr.Zero || game.World == null) return;

        // Layer 0: Terrain
        GetTerrainUVs(Bitmask, out float u0, out float v0, out float u1, out float v1);
        Native.TileMap_SetTile(game.TileMap, PositionX, PositionY, 0, FactoryResources.GetTerrainTexture(Type), u0, v0, u1, v1, 1, 1, 1, 1, 0);

        // Layer 1: Ore
        nint oreTex = FactoryResources.GetOreTexture(OreType);
        if (oreTex != IntPtr.Zero)
            Native.TileMap_SetTile(game.TileMap, PositionX, PositionY, 1, oreTex, 0, 0, 1, 1, 1, 1, 1, 1, 0);
        else
            Native.TileMap_SetTile(game.TileMap, PositionX, PositionY, 1, IntPtr.Zero, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        // Layer 2: Structure
        nint structTex = FactoryResources.GetStructureTexture(Structure, Tier);
        float rotation = 0.0f;
        if (Structure == FactoryStructure.Wall)
        {
            switch (Direction)
            {
                case Direction.East: rotation = -1.5708f; break;
                case Direction.South: rotation = 3.14159f; break;
                case Direction.West: rotation = 1.5708f; break;
            }
        }

        if (Structure == FactoryStructure.ConveyorBelt)
        {
            // Use ConveyorSystem
            game.ConveyorSystem.PlaceConveyor(PositionX, PositionY, Tier, Direction);
            game.BuildingSystem.RemoveBuilding(PositionX, PositionY); // Ensure no building

            // Don't render in TileMap
            Native.TileMap_SetTile(game.TileMap, PositionX, PositionY, 2, IntPtr.Zero, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }
        else if (Structure == FactoryStructure.Miner || Structure == FactoryStructure.Storage || Structure == FactoryStructure.FarmPlot)
        {
            // Use BuildingSystem
            game.BuildingSystem.PlaceBuilding(PositionX, PositionY, Structure, Direction, Tier);
            game.ConveyorSystem.RemoveConveyor(PositionX, PositionY); // Ensure no conveyor

            if (structTex != IntPtr.Zero)
            {
                // Use the texture
                Native.TileMap_SetTile(game.TileMap, PositionX, PositionY, 2, structTex, 0, 0, 1, 1, 1, 1, 1, 1, rotation);
            }
            else
            {
                // Fallback: Use a generic square (maybe the conveyor texture?) but we want color.
                nint baseTex = FactoryResources.GetTerrainTexture(FactoryTerrain.Concrete);

                if (Structure == FactoryStructure.Miner)
                {
                    Native.TileMap_SetTile(game.TileMap, PositionX, PositionY, 2, baseTex, 0, 0, 1, 1, 0.6f, 0.2f, 0.6f, 1.0f, 0);
                }
                else
                    Native.TileMap_SetTile(game.TileMap, PositionX, PositionY, 2, baseTex, 0, 0, 1, 1, 0.6f, 0.4f, 0.2f, 1.0f, 0);
            }
        }
        else
        {
            // Remove from systems
            game.ConveyorSystem.RemoveConveyor(PositionX, PositionY);
            game.BuildingSystem.RemoveBuilding(PositionX, PositionY);

            if (structTex != IntPtr.Zero)
                Native.TileMap_SetTile(game.TileMap, PositionX, PositionY, 2, structTex, 0, 0, 1, 1, 1, 1, 1, 1, rotation);
            else
                Native.TileMap_SetTile(game.TileMap, PositionX, PositionY, 2, IntPtr.Zero, 0, 0, 0, 0, 0, 0, 0, 0, 0);
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
