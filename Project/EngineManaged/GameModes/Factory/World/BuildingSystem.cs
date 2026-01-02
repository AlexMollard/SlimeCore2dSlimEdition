using System;
using System.Collections.Generic;
using EngineManaged.Numeric;
using SlimeCore.GameModes.Factory.Actors;
using SlimeCore.GameModes.Factory.Items;
using SlimeCore.GameModes.Factory.Buildings;
using SlimeCore.GameModes.Factory.Buildings.Behaviors;
using SlimeCore.Source.Core;
using SlimeCore.Source.World.Actors;

namespace SlimeCore.GameModes.Factory.World;

public class BuildingSystem : IDisposable
{
    private Dictionary<int, BuildingInstance> _buildings = new();
    private FactoryWorld _world;
    private ConveyorSystem _conveyorSystem;
    private FactoryGame _game; 

    public BuildingSystem(FactoryGame game, FactoryWorld world, ConveyorSystem conveyorSystem)
    {
        _game = game;
        _world = world;
        _conveyorSystem = conveyorSystem;

        // Initialize registries
        ItemRegistry.Initialize();
        BuildingRegistry.Initialize();
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

    public void PlaceBuilding(int x, int y, string buildingId, Direction dir)
    {
        var def = BuildingRegistry.Get(buildingId);
        if (def == null) return;

        int idx = y * _world.Width() + x;
        var instance = new BuildingInstance(def, x, y, dir);
        
        // Initialize behaviors
        foreach (var comp in def.Components)
        {
            IBuildingBehavior? behavior = null;
            switch (comp.Type)
            {
                case "Miner": behavior = new MinerBehavior(comp.Properties); break;
                case "Storage": behavior = new StorageBehavior(comp.Properties); break;
                case "Furnace": behavior = new FurnaceBehavior(comp.Properties); break;
                case "Farm": behavior = new FarmBehavior(comp.Properties); break;
                case "Generator": behavior = new GeneratorBehavior(comp.Properties); break;
            }
            
            if (behavior != null)
            {
                instance.Behaviors.Add(behavior);
                behavior.OnPlace(instance, _game);
            }
        }

        _buildings[idx] = instance;
    }

    public void RemoveBuilding(int x, int y)
    {
        int idx = y * _world.Width() + x;
        if (_buildings.TryGetValue(idx, out var b))
        {
            foreach(var behavior in b.Behaviors)
            {
                behavior.OnRemove(b, _game);
            }
            _buildings.Remove(idx);
        }
    }

    public bool TryAcceptItem(int x, int y, string itemId)
    {
        int idx = y * _world.Width() + x;
        if (_buildings.TryGetValue(idx, out var b))
        {
            foreach(var behavior in b.Behaviors)
            {
                if (behavior.TryAcceptItem(b, itemId, _game))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public Dictionary<string, int> GetBuildingInventory(int x, int y)
    {
        int idx = y * _world.Width() + x;
        if (_buildings.TryGetValue(idx, out var b))
        {
            return b.Inventory;
        }
        return new Dictionary<string, int>();
    }

    public void Update(float dt)
    {
        var keys = new List<int>(_buildings.Keys);
        
        foreach (int key in keys)
        {
            if (_buildings.TryGetValue(key, out var b))
            {
                foreach (var behavior in b.Behaviors)
                {
                    behavior.Update(b, dt, _game);
                }
            }
        }
    }
}

