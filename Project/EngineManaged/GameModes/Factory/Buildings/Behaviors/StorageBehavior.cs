using System.Collections.Generic;
using System.Text.Json;
using SlimeCore.GameModes.Factory.World;

namespace SlimeCore.GameModes.Factory.Buildings.Behaviors;

public class StorageBehavior : IBuildingBehavior
{
    private float _timer;
    private float _outputRate;
    private int _capacity;

    public StorageBehavior(Dictionary<string, object>? props)
    {
        _outputRate = 0.5f;
        _capacity = 100;

        if (props != null)
        {
            if (props.TryGetValue("OutputRate", out var rateObj))
            {
                 if (rateObj is JsonElement je) _outputRate = (float)je.GetDouble();
                 else if (rateObj is double d) _outputRate = (float)d;
                 else if (rateObj is float f) _outputRate = f;
            }

            if (props.TryGetValue("Capacity", out var capObj))
            {
                 if (capObj is JsonElement je) _capacity = je.GetInt32();
                 else if (capObj is int i) _capacity = i;
                 else if (capObj is long l) _capacity = (int)l;
            }
        }
    }

    public void Update(BuildingInstance instance, float dt, FactoryGame game)
    {
        _timer += dt;
        if (_timer >= _outputRate)
        {
            _timer = 0;
            
            string? itemToOutput = null;
            foreach (var kvp in instance.Inventory)
            {
                if (kvp.Value > 0)
                {
                    itemToOutput = kvp.Key;
                    break;
                }
            }

            if (itemToOutput != null)
            {
                if (BuildingUtils.TryOutputToConveyor(game, instance, itemToOutput))
                {
                    instance.Inventory[itemToOutput]--;
                    if (instance.Inventory[itemToOutput] <= 0)
                    {
                        instance.Inventory.Remove(itemToOutput);
                    }
                }
            }
        }
    }

    public void OnPlace(BuildingInstance instance, FactoryGame game) {}
    public void OnRemove(BuildingInstance instance, FactoryGame game) {}

    public bool TryAcceptItem(BuildingInstance instance, string itemId, FactoryGame game)
    {
        // Check capacity (total items)
        int totalItems = 0;
        foreach (var kvp in instance.Inventory) totalItems += kvp.Value;

        if (totalItems < _capacity)
        {
            if (!instance.Inventory.ContainsKey(itemId))
            {
                instance.Inventory[itemId] = 0;
            }
            instance.Inventory[itemId]++;
            return true;
        }
        return false;
    }
}
