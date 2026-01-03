using SlimeCore.GameModes.Factory;
using SlimeCore.GameModes.Idle;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SlimeCore.GameModes.Idle.Store;

public static class StoreRegistry
{
    private static Dictionary<string, StoreDefinition> _store = new();
    public static void Register(StoreDefinition store)
    {
        if (_store.ContainsKey(store.Id))
        {
            return;
        }

        
        if (store.Texture == IntPtr.Zero && !string.IsNullOrEmpty(store.TexturePath))
        {
            store.Texture = FactoryResources.GetOrCreateTexture($"{store.Id}", store.TexturePath);
        }

        _store[store.Id] = store;
        _store[store.Id].Cost = store.BaseCost;

    }
    
    public static StoreDefinition? Get(string id)
    {
        return _store.TryGetValue(id, out var b) ? b : null;
    }

    public static IEnumerable<StoreDefinition> GetAll() => _store.Values;

    public static void Initialize()
    {
        // Load from ResourceManager
        IntPtr ptr = NativeMethods.Resources_LoadText("store_data", "Data/idlestore.json");
        if (ptr != IntPtr.Zero)
        {
            try
            {
                string json = System.Runtime.InteropServices.Marshal.PtrToStringUTF8(ptr);
                var store = JsonSerializer.Deserialize<List<StoreDefinition>>(json);
                if (store != null)
                {
                    Console.WriteLine($"Loaded {store.Count} Store from ResourceManager:");
                    foreach (var b in store)
                    {
                        Register(b);
                        Console.WriteLine($" - {b.Name} ({b.Id})");
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse idleStore.json: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Failed to load idleStore.json from ResourceManager");
        }
    
        Console.WriteLine("Warning: idleStore.json not found.");
    }

}