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
            if (instance.InventoryCount > 0 && !string.IsNullOrEmpty(instance.InventoryItemId))
            {
                if (BuildingUtils.TryOutputToConveyor(game, instance, instance.InventoryItemId))
                {
                    instance.InventoryCount--;
                    if (instance.InventoryCount == 0) instance.InventoryItemId = "";
                }
            }
        }
    }

    public void OnPlace(BuildingInstance instance, FactoryGame game) {}
    public void OnRemove(BuildingInstance instance, FactoryGame game) {}
}
