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

    private Dictionary<string, string> _oreMapping = new();

    public MinerBehavior(Dictionary<string, object>? props)
    {
        _speed = 1.0f;
        _tier = 1;

        // Default Mapping
        _oreMapping["Iron"] = "iron_ore";
        _oreMapping["Copper"] = "copper_ore";
        _oreMapping["Coal"] = "coal";
        _oreMapping["Gold"] = "gold_ore";

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

            if (props.TryGetValue("OreMapping", out var mapObj))
            {
                if (mapObj is JsonElement je && je.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in je.EnumerateObject())
                    {
                        _oreMapping[prop.Name] = prop.Value.GetString() ?? "";
                    }
                }
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
    public bool TryAcceptItem(BuildingInstance instance, string itemId, FactoryGame game) => false;

    private string? GetItemIdFromOre(FactoryOre ore)
    {
        string key = ore.ToString();
        return _oreMapping.TryGetValue(key, out var id) ? id : null;
    }
}
