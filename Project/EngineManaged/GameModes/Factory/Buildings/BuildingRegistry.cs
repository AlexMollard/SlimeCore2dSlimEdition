using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SlimeCore.GameModes.Factory;

namespace SlimeCore.GameModes.Factory.Buildings;

public static class BuildingRegistry
{
    private static Dictionary<string, BuildingDefinition> _buildings = new();

    public static void Register(BuildingDefinition building)
    {
        if (_buildings.ContainsKey(building.Id))
        {
            return;
        }

        if (building.Texture == IntPtr.Zero && !string.IsNullOrEmpty(building.TexturePath))
        {
            building.Texture = FactoryResources.GetOrCreateTexture($"building_{building.Id}", building.TexturePath);
        }

        _buildings[building.Id] = building;
    }

    public static BuildingDefinition? Get(string id)
    {
        return _buildings.TryGetValue(id, out var b) ? b : null;
    }

    public static IEnumerable<BuildingDefinition> GetAll() => _buildings.Values;

    public static void Initialize()
    {
        // Load from ResourceManager
        IntPtr ptr = NativeMethods.Resources_LoadText("buildings_data", "Data/buildings.json");
        if (ptr != IntPtr.Zero)
        {
            try 
            {
                string json = System.Runtime.InteropServices.Marshal.PtrToStringUTF8(ptr);
                var buildings = JsonSerializer.Deserialize<List<BuildingDefinition>>(json);
                if (buildings != null)
                {
                    Console.WriteLine($"Loaded {buildings.Count} buildings from ResourceManager:");
                    foreach (var b in buildings)
                    {
                        Register(b);
                        Console.WriteLine($" - {b.Name} ({b.Id})");
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse buildings.json: {ex.Message}");
            }
        }
        else
        {
             Console.WriteLine("Failed to load buildings.json from ResourceManager");
        }
        
        Console.WriteLine("Warning: buildings.json not found.");
    }
}
