using System.Collections.Generic;
using System.Text.Json;
using SlimeCore.GameModes.Factory.World;

namespace SlimeCore.GameModes.Factory.Buildings.Behaviors;

public class FarmBehavior : IBuildingBehavior
{
    private float _timer;
    private float _growthTime;

    public FarmBehavior(Dictionary<string, object>? props)
    {
        _growthTime = 1.0f;

        if (props != null)
        {
            if (props.TryGetValue("GrowthTime", out object? timeObj))
            {
                 if (timeObj is JsonElement je) _growthTime = (float)je.GetDouble();
                 else if (timeObj is double d) _growthTime = (float)d;
                 else if (timeObj is float f) _growthTime = f;
            }
        }
    }

    public void Update(BuildingInstance instance, float dt, FactoryGame game)
    {
        _timer += dt;
        if (_timer >= _growthTime)
        {
            _timer = 0;
            BuildingUtils.TryOutputToConveyor(game, instance, "vegetable");
        }
    }

    public void OnPlace(BuildingInstance instance, FactoryGame game) {}
    public void OnRemove(BuildingInstance instance, FactoryGame game) {}
    public bool TryAcceptItem(BuildingInstance instance, string itemId, FactoryGame game) => false;
}
