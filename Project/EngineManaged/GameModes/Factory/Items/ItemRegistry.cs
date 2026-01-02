using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SlimeCore.GameModes.Factory;

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

        // Load texture if path is provided
        if (item.IconTexture == IntPtr.Zero && !string.IsNullOrEmpty(item.IconPath))
        {
            item.IconTexture = FactoryResources.GetOrCreateTexture($"item_{item.Id}", item.IconPath);
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
        // Load from ResourceManager
        IntPtr ptr = NativeMethods.Resources_LoadText("items_data", "Data/items.json");
        if (ptr != IntPtr.Zero)
        {
            try 
            {
                string json = System.Runtime.InteropServices.Marshal.PtrToStringUTF8(ptr);
                var items = JsonSerializer.Deserialize<List<ItemDefinition>>(json);
                if (items != null)
                {
                    Console.WriteLine($"Loaded {items.Count} items from ResourceManager:");
                    foreach (var item in items)
                    {
                        Register(item);
                        Console.WriteLine($" - {item.Name} ({item.Id})");
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse items.json: {ex.Message}");
            }
        }
        else
        {
             Console.WriteLine("Failed to load items.json from ResourceManager");
        }

        // Fallback if no file found
        Console.WriteLine("Warning: items.json not found, using fallback defaults.");
        Register(new ItemDefinition { Id = "iron_ore", Name = "Iron Ore", Description = "Raw iron ore.", IconTexture = FactoryResources.TexItemIronOre });
        Register(new ItemDefinition { Id = "copper_ore", Name = "Copper Ore", Description = "Raw copper ore.", IconTexture = FactoryResources.TexItemCopperOre });
        Register(new ItemDefinition { Id = "coal", Name = "Coal", Description = "Fuel.", IconTexture = FactoryResources.TexItemCoal });
        Register(new ItemDefinition { Id = "gold_ore", Name = "Gold Ore", Description = "Shiny.", IconTexture = FactoryResources.TexItemGoldOre });
        Register(new ItemDefinition { Id = "stone", Name = "Stone", Description = "Basic building material.", IconTexture = FactoryResources.TexItemStone });
        Register(new ItemDefinition { Id = "vegetable", Name = "Vegetable", Description = "Food.", IconTexture = FactoryResources.TexItemVegetable });
        Register(new ItemDefinition { Id = "mutton", Name = "Mutton", Description = "Dropped by sheep.", IconTexture = FactoryResources.TexItemVegetable }); // Reusing texture
    }
}
