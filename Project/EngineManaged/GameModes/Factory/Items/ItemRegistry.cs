using System.Collections.Generic;

namespace SlimeCore.GameModes.Factory.Items;

public static class ItemRegistry
{
    private static Dictionary<string, ItemDefinition> _items = new();

    public static void Register(ItemDefinition item)
    {
        if (_items.ContainsKey(item.Id))
        {
            // Log warning?
            return;
        }
        _items[item.Id] = item;
    }

    public static ItemDefinition? Get(string id)
    {
        return _items.TryGetValue(id, out var item) ? item : null;
    }

    public static IEnumerable<ItemDefinition> GetAll() => _items.Values;

    // Initialize default items for testing
    public static void Initialize()
    {
        Register(new ItemDefinition { Id = "iron_ore", Name = "Iron Ore", Description = "Raw iron ore.", IconTexture = FactoryResources.TexItemIronOre });
        Register(new ItemDefinition { Id = "copper_ore", Name = "Copper Ore", Description = "Raw copper ore.", IconTexture = FactoryResources.TexItemCopperOre });
        Register(new ItemDefinition { Id = "coal", Name = "Coal", Description = "Fuel.", IconTexture = FactoryResources.TexItemCoal });
        Register(new ItemDefinition { Id = "gold_ore", Name = "Gold Ore", Description = "Shiny.", IconTexture = FactoryResources.TexItemGoldOre });
        Register(new ItemDefinition { Id = "stone", Name = "Stone", Description = "Basic building material.", IconTexture = FactoryResources.TexItemStone });
        Register(new ItemDefinition { Id = "vegetable", Name = "Vegetable", Description = "Food.", IconTexture = FactoryResources.TexItemVegetable });
        Register(new ItemDefinition { Id = "mutton", Name = "Mutton", Description = "Dropped by sheep.", IconTexture = FactoryResources.TexItemVegetable }); // Reusing texture
    }
}
