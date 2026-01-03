using System.Collections.Generic;
using System.Text.Json;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.GameModes.Factory.Items;

namespace SlimeCore.GameModes.Factory.Buildings.Behaviors;

public class FurnaceBehavior : IBuildingBehavior
{
    private float _timer;
    private float _processTime;
    private Dictionary<string, string> _recipes = new(); // Input -> Output
    
    public FurnaceBehavior(Dictionary<string, object>? props)
    {
        _processTime = 2.0f;

        // Default Recipes
        _recipes["iron_ore"] = "iron_ingot";
        _recipes["copper_ore"] = "copper_ingot";
        _recipes["gold_ore"] = "gold_ingot";

        if (props != null)
        {
            if (props.TryGetValue("ProcessTime", out object? timeObj))
            {
                 if (timeObj is JsonElement je) _processTime = (float)je.GetDouble();
                 else if (timeObj is double d) _processTime = (float)d;
                 else if (timeObj is float f) _processTime = f;
            }

            if (props.TryGetValue("Recipes", out object? recipesObj))
            {
                if (recipesObj is JsonElement je && je.ValueKind == JsonValueKind.Object)
                {
                    _recipes.Clear();
                    foreach (var prop in je.EnumerateObject())
                    {
                        _recipes[prop.Name] = prop.Value.GetString() ?? "";
                    }
                }
            }
        }
    }

    public void Update(BuildingInstance instance, float dt, FactoryGame game)
    {
        // Output Logic: Try to output any ingots we have
        foreach (var kvp in instance.Inventory)
        {
            // Check if item is an output of any recipe
            bool isOutput = false;
            foreach(string r in _recipes.Values)
            {
                if (r == kvp.Key) 
                {
                    isOutput = true;
                    break;
                }
            }

            if (isOutput && kvp.Value > 0)
            {
                if (BuildingUtils.TryOutputToConveyor(game, instance, kvp.Key))
                {
                    instance.Inventory[kvp.Key]--;
                    if (instance.Inventory[kvp.Key] <= 0)
                    {
                        instance.Inventory.Remove(kvp.Key);
                    }
                    return; // Only output one thing per tick
                }
            }
        }

        // Processing Logic: Need Coal + Ore
        if (instance.Inventory.TryGetValue("coal", out int coalCount) && coalCount > 0)
        {
            foreach (var recipe in _recipes)
            {
                string inputOre = recipe.Key;
                string outputIngot = recipe.Value;
                
                if (instance.Inventory.TryGetValue(inputOre, out int oreCount) && oreCount > 0)
                {
                    _timer += dt;
                    if (_timer >= _processTime)
                    {
                        _timer = 0;
                        
                        // Consume inputs
                        instance.Inventory["coal"]--;
                        if (instance.Inventory["coal"] <= 0) instance.Inventory.Remove("coal");
                        
                        instance.Inventory[inputOre]--;
                        if (instance.Inventory[inputOre] <= 0) instance.Inventory.Remove(inputOre);
                        
                        // Produce output
                        if (!instance.Inventory.ContainsKey(outputIngot)) instance.Inventory[outputIngot] = 0;
                        instance.Inventory[outputIngot]++;
                    }
                    return; // Only process one recipe at a time
                }
            }
        }
        else
        {
            _timer = 0; // Reset timer if no fuel
        }
    }

    public void OnPlace(BuildingInstance instance, FactoryGame game) {}
    public void OnRemove(BuildingInstance instance, FactoryGame game) {}

    public bool TryAcceptItem(BuildingInstance instance, string itemId, FactoryGame game)
    {
        // Accept Coal (Fuel)
        if (itemId == "coal")
        {
            if (!instance.Inventory.ContainsKey(itemId)) instance.Inventory[itemId] = 0;
            if (instance.Inventory[itemId] < 20) // Limit fuel buffer
            {
                instance.Inventory[itemId]++;
                return true;
            }
            return false;
        }

        // Accept Valid Ores
        if (_recipes.ContainsKey(itemId))
        {
            if (!instance.Inventory.ContainsKey(itemId)) instance.Inventory[itemId] = 0;
            if (instance.Inventory[itemId] < 20) // Limit input buffer
            {
                instance.Inventory[itemId]++;
                return true;
            }
        }

        return false;
    }
}
