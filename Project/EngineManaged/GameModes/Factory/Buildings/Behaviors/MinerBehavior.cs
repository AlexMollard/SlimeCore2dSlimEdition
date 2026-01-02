using System.Collections.Generic;
using System.Text.Json;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.GameModes.Factory.Items;
using EngineManaged.Numeric;
using SlimeCore.GameModes.Factory.Actors;

namespace SlimeCore.GameModes.Factory.Buildings.Behaviors;

public class MinerBehavior : IBuildingBehavior
{
    private float _timer;
    private float _speed;
    private int _tier;

    public MinerBehavior(Dictionary<string, object>? props)
    {
        _speed = 1.0f;
        _tier = 1;

        if (props != null)
        {
            if (props.TryGetValue("Speed", out var speedObj))
            {
                 if (speedObj is JsonElement je) _speed = (float)je.GetDouble();
                 else if (speedObj is double d) _speed = (float)d;
                 else if (speedObj is float f) _speed = f;
            }

            if (props.TryGetValue("Tier", out var tierObj))
            {
                 if (tierObj is JsonElement je) _tier = je.GetInt32();
                 else if (tierObj is int i) _tier = i;
                 else if (tierObj is long l) _tier = (int)l;
            }
        }
    }

    public void Update(BuildingInstance instance, float dt, FactoryGame game)
    {
        _timer += dt;
        float miningTime = 1.0f / _speed;
        
        if (_timer >= miningTime) 
        {
            _timer = 0;
            var tile = game.World[instance.X, instance.Y];
            if (tile.OreType != FactoryOre.None)
            {
                var itemId = GetItemIdFromOre(tile.OreType);
                if (itemId != null)
                {
                    BuildingUtils.TryOutputToConveyor(game, instance, itemId);
                }
            }
        }
    }

    public void OnPlace(BuildingInstance instance, FactoryGame game) {}
    public void OnRemove(BuildingInstance instance, FactoryGame game) {}

    private string? GetItemIdFromOre(FactoryOre ore)
    {
        return ore switch
        {
            FactoryOre.Iron => "iron_ore",
            FactoryOre.Copper => "copper_ore",
            FactoryOre.Coal => "coal",
            FactoryOre.Gold => "gold_ore",
            _ => null
        };
    }
}
