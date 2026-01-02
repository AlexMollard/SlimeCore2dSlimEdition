using System.Collections.Generic;
using System.Text.Json;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.GameModes.Factory.Buildings;

namespace SlimeCore.GameModes.Factory.Buildings.Behaviors;

public class GeneratorBehavior : IBuildingBehavior
{
    private float _timer;
    private float _interval;
    private string _outputItemId;

    public GeneratorBehavior(Dictionary<string, object>? props)
    {
        _interval = 1.0f;
        _outputItemId = "stone"; // Default

        if (props != null)
        {
            if (props.TryGetValue("Interval", out object? intervalObj))
            {
                 if (intervalObj is JsonElement je) _interval = (float)je.GetDouble();
                 else if (intervalObj is double d) _interval = (float)d;
                 else if (intervalObj is float f) _interval = f;
            }

            if (props.TryGetValue("OutputItem", out object? itemObj))
            {
                 if (itemObj is JsonElement je) _outputItemId = je.GetString() ?? "stone";
                 else if (itemObj is string s) _outputItemId = s;
            }
        }
    }

    public void Update(BuildingInstance instance, float dt, FactoryGame game)
    {
        _timer += dt;
        if (_timer >= _interval)
        {
            _timer = 0;
            BuildingUtils.TryOutputToConveyor(game, instance, _outputItemId);
        }
    }

    public void OnPlace(BuildingInstance instance, FactoryGame game) { }
    public void OnRemove(BuildingInstance instance, FactoryGame game) { }
    public bool TryAcceptItem(BuildingInstance instance, string itemId, FactoryGame game) => false;
}
