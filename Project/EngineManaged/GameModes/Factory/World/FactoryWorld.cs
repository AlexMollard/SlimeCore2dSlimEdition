using EngineManaged.Numeric;
using SlimeCore.Source.Common;
using SlimeCore.Source.World.Grid;
using SlimeCore.Source.World.Grid.Pathfinding;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SlimeCore.GameModes.Factory.World;

public class FactoryWorld : GridSystem<FactoryGame, FactoryTerrain, FactoryTileOptions, FactoryTile>, IWorldGrid
{
    [NotMapped]
    public float Zoom { get; set; } = 1.0f;

    public bool ShouldRender { get; set; }

    public FactoryWorld(int worldWidth, 
        int worldHeight, 
        FactoryTerrain init_type, 
        float zoom,
        int TickBudget) 
        : base(worldWidth, worldHeight, init_type, TickBudget)
    {
        Zoom = zoom;
    }

    public void Initialize(int viewWidth, int viewHeight)
    {
        // No initialization needed for batch rendering
    }

    public void Destroy()
    {
        // No destruction needed
    }



    public override FactoryTile? Set(Vec2i position, Action<FactoryTileOptions> config)
    {
        var changed = base.Set(position, config);
        if (!changed.Rendered)
        {
            Register(changed);
        }
        return changed;
    }

    public override void Tick(FactoryGame mode, float deltaTime)
    {
        //Logger.Trace($"There are {_actionQueue.Count} items needing to be rendered");
        base.Tick(mode, deltaTime);
        if (ShouldRender)
        {
            //Logger.Trace($"RENDER COMPLETE");
            Native.TileMap_UpdateMesh(mode.TileMap);
        }
    }

    public void UpdateTileConnectivity(FactoryGame game, int x, int y) => UpdateTileConnectivity(game, new Vec2i(x, y));

    public void UpdateTileConnectivity(FactoryGame game, Vec2i pos)
    {
        if (!Grid.TryGetValue(pos, out var tile)) return;

        // Update Terrain Bitmask
        int mask = 0;
        if (IsSameTerrain(pos, Direction.North)) mask |= (int)ConnectivityMask.North;
        if (IsSameTerrain(pos, Direction.East)) mask |= (int)ConnectivityMask.East;
        if (IsSameTerrain(pos, Direction.South)) mask |= (int)ConnectivityMask.South;
        if (IsSameTerrain(pos, Direction.West)) mask |= (int)ConnectivityMask.West;
        
        // We can also check diagonals for full marching squares if needed
        // if (IsSameTerrain(pos, Direction.North) && IsSameTerrain(pos, Direction.East) && IsSameTerrain(pos + new Vec2i(1, 1))) ...

        tile.Bitmask = mask;

        // Update Conveyor Logic if applicable
        if (tile.Structure == FactoryStructure.ConveyorBelt)
        {
            UpdateConveyorShape(tile, pos);
        }
        tile.UpdateTile(game);
    }

    private void UpdateConveyorShape(FactoryTile tile, Vec2i pos)
    {
        // Check inputs
        bool inputNorth = IsConveyorInput(pos, Direction.North);
        bool inputEast = IsConveyorInput(pos, Direction.East);
        bool inputSouth = IsConveyorInput(pos, Direction.South);
        bool inputWest = IsConveyorInput(pos, Direction.West);

        // Determine shape based on inputs and my direction
        // This is a simplified logic, can be expanded
        // We store the "Input Mask" in the upper bits of Bitmask or a separate field
        // For now, let's just use the Bitmask field for Conveyors too, but with different meaning
        
        int inputMask = 0;
        if (inputNorth) inputMask |= (int)ConnectivityMask.North;
        if (inputEast) inputMask |= (int)ConnectivityMask.East;
        if (inputSouth) inputMask |= (int)ConnectivityMask.South;
        if (inputWest) inputMask |= (int)ConnectivityMask.West;

        tile.Bitmask = inputMask;
    }

    private bool IsConveyorInput(Vec2i pos, Direction dir)
    {
        var neighborPos = pos + dir.GetOffset();
        if (Grid.TryGetValue(neighborPos, out var neighbor))
        {
            if (neighbor.Structure == FactoryStructure.ConveyorBelt)
            {
                // Check if neighbor points to us
                // If neighbor is North, it must point South
                return neighbor.Direction == dir.Opposite();
            }
        }
        return false;
    }

    public void UpdateNeighbors(FactoryGame game, Vec2i pos)
    {
        UpdateTileConnectivity(game, pos);
        UpdateTileConnectivity(game, pos + Direction.North.GetOffset());
        UpdateTileConnectivity(game, pos + Direction.East.GetOffset());
        UpdateTileConnectivity(game, pos + Direction.South.GetOffset());
        UpdateTileConnectivity(game, pos + Direction.West.GetOffset());
    }

    private bool IsSameTerrain(Vec2i pos, Direction dir)
    {
        var neighborPos = pos + dir.GetOffset();
        if (Grid.TryGetValue(neighborPos, out var neighbor))
        {
            // For now, just check if it's the same terrain type
            // We might want to group types (e.g. all "solid" blocks connect)
            return Grid[pos].Type == neighbor.Type;
        }
        return false;
    }

    private bool IsSameTerrain(Vec2i pos, Vec2i offset)
    {
        if (Grid.TryGetValue(pos + offset, out var neighbor))
        {
            return Grid[pos].Type == neighbor.Type;
        }
        return false;
    }

    public bool IsBlocked(Vec2i tile)
    {
        if (!InBounds(tile))
        {
            return true;
        }
        return Grid[tile].IsBlocked();
    }

    public bool IsLiquid(Vec2i tile)
    {
        return Grid[tile].IsLiquid();
    }
}
