using System;
using System.Collections.Generic;
using EngineManaged.Numeric;
using SlimeCore.Source.Core;
using SlimeCore.Source.World.Actors;

namespace SlimeCore.GameModes.Factory.World;

public class BuildingSystem : IDisposable
{
    private struct Building
    {
        public FactoryStructure Type;
        public int X, Y;
        public float Timer;
        public int InventoryCount;
        public FactoryItemType InventoryItem;
        public Direction Direction;
        public int Tier;
    }

    private Dictionary<int, Building> _buildings = new();
    private FactoryWorld _world;
    private ConveyorSystem _conveyorSystem;

    public BuildingSystem(FactoryWorld world, ConveyorSystem conveyorSystem)
    {
        _world = world;
        _conveyorSystem = conveyorSystem;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    ~BuildingSystem()
    {
        Dispose(false);
    }

    public void PlaceBuilding(int x, int y, FactoryStructure type, Direction dir, int tier = 1)
    {
        int idx = y * _world.Width() + x;
        _buildings[idx] = new Building 
        { 
            Type = type, 
            X = x, 
            Y = y, 
            Direction = dir,
            Timer = 0,
            Tier = tier
        };
    }

    public void RemoveBuilding(int x, int y)
    {
        int idx = y * _world.Width() + x;
        _buildings.Remove(idx);
    }

    public bool TryAcceptItem(int x, int y, FactoryItemType item)
    {
        int idx = y * _world.Width() + x;
        if (_buildings.TryGetValue(idx, out var b))
        {
            if (b.Type == FactoryStructure.Storage)
            {
                // Accept item
                b.InventoryCount++;
                b.InventoryItem = item; // Simple storage: just stores last type or mixed
                _buildings[idx] = b;
                return true;
            }
        }
        return false;
    }

    public (int count, FactoryItemType item) GetBuildingInventory(int x, int y)
    {
        int idx = y * _world.Width() + x;
        if (_buildings.TryGetValue(idx, out var b))
        {
            return (b.InventoryCount, b.InventoryItem);
        }
        return (0, FactoryItemType.None);
    }

    public void Update(float dt)
    {
        // Iterate over keys to avoid modification issues if we were removing (we aren't here)
        // But we are modifying the struct in the dictionary
        var keys = new List<int>(_buildings.Keys);
        
        foreach (int key in keys)
        {
            var b = _buildings[key];
            
            if (b.Type == FactoryStructure.Miner)
            {
                // Mining logic
                b.Timer += dt;
                float miningTime = 1.0f / b.Tier; // Tier 1 = 1s, Tier 2 = 0.5s, Tier 3 = 0.33s
                
                if (b.Timer >= miningTime) 
                {
                    b.Timer = 0;
                    // Check ore
                    var tile = _world[b.X, b.Y];
                    if (tile.OreType != FactoryOre.None)
                    {
                        var itemType = GetItemFromOre(tile.OreType);
                        TryOutputToConveyor(b.X, b.Y, itemType);
                    }
                }
            }
            else if (b.Type == FactoryStructure.Storage)
            {
                b.Timer += dt;
                if (b.Timer >= 0.5f) // Output rate
                {
                    b.Timer = 0;
                    if (b.InventoryCount > 0)
                    {
                        if (TryOutputToConveyor(b.X, b.Y, b.InventoryItem))
                        {
                            b.InventoryCount--;
                            if (b.InventoryCount == 0) b.InventoryItem = FactoryItemType.None;
                            _buildings[key] = b;
                        }
                    }
                }
            }
            else if (b.Type == FactoryStructure.FarmPlot)
            {
                b.Timer += dt;
                float growingTime = 1.0f / b.Tier; // Tier 1 = 1s, Tier 2 = 0.5s, Tier 3 = 0.33s
                
                if (b.Timer >= growingTime) 
                {
                    b.Timer = 0;
                    TryOutputToConveyor(b.X, b.Y, FactoryItemType.Vegetable);
                }
            }
            
            _buildings[key] = b;
        }
    }

    private bool TryOutputToConveyor(int bx, int by, FactoryItemType itemType)
    {
        var dirs = new[] { Direction.North, Direction.East, Direction.South, Direction.West };
        foreach (var dir in dirs)
        {
            var (nx, ny) = GetNeighbor(bx, by, dir);
            var conveyorDir = _conveyorSystem.GetConveyorDirection(nx, ny);
            
            // Only output if conveyor is pointing AWAY from building (same as neighbor direction)
            if (conveyorDir.HasValue && conveyorDir.Value == dir)
            {
                if (_conveyorSystem.TryAddItem(nx, ny, itemType))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private FactoryItemType GetItemFromOre(FactoryOre ore)
    {
        return ore switch
        {
            FactoryOre.Iron => FactoryItemType.IronOre,
            FactoryOre.Copper => FactoryItemType.CopperOre,
            FactoryOre.Coal => FactoryItemType.Coal,
            FactoryOre.Gold => FactoryItemType.GoldOre,
            _ => FactoryItemType.None
        };
    }

    private (int x, int y) GetNeighbor(int x, int y, Direction dir)
    {
        return dir switch
        {
            Direction.North => (x, y + 1),
            Direction.East => (x + 1, y),
            Direction.South => (x, y - 1),
            Direction.West => (x - 1, y),
            _ => (x, y)
        };
    }
}
