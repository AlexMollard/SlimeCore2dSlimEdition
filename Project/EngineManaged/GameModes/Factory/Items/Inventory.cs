using System;
using System.Collections.Generic;
using System.Linq;

namespace SlimeCore.GameModes.Factory.Items;

public class Inventory
{
    public List<ItemStack> Slots { get; private set; } = new();
    public int MaxSlots { get; set; } = 10;

    public event Action? OnInventoryChanged;

    public bool AddItem(ItemDefinition item, int count)
    {
        int remaining = count;

        // 1. Try to stack with existing items
        foreach (var slot in Slots)
        {
            if (slot.Item.Id == item.Id && slot.Count < slot.Item.MaxStack)
            {
                int space = slot.Item.MaxStack - slot.Count;
                int toAdd = Math.Min(space, remaining);
                slot.Count += toAdd;
                remaining -= toAdd;

                if (remaining <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        // 2. Add to new slots
        while (remaining > 0)
        {
            if (Slots.Count >= MaxSlots)
            {
                // Inventory full, could not add all items
                // In a real game we might drop the rest on the ground
                OnInventoryChanged?.Invoke();
                return false; 
            }

            int toAdd = Math.Min(item.MaxStack, remaining);
            Slots.Add(new ItemStack(item, toAdd));
            remaining -= toAdd;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(string itemId, int count)
    {
        int total = GetItemCount(itemId);
        if (total < count) return false;

        int remainingToRemove = count;
        
        // Iterate backwards to safely remove empty slots if needed (though we might want to keep empty slots if we had a fixed grid)
        // For a dynamic list, iterating normally is fine if we handle removal carefully.
        // Let's just iterate and modify.
        
        for (int i = Slots.Count - 1; i >= 0; i--)
        {
            var slot = Slots[i];
            if (slot.Item.Id == itemId)
            {
                int toTake = Math.Min(slot.Count, remainingToRemove);
                slot.Count -= toTake;
                remainingToRemove -= toTake;

                if (slot.Count <= 0)
                {
                    Slots.RemoveAt(i);
                }

                if (remainingToRemove <= 0) break;
            }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public int GetItemCount(string itemId)
    {
        return Slots.Where(s => s.Item.Id == itemId).Sum(s => s.Count);
    }
    
    public bool HasItem(string itemId, int count)
    {
        return GetItemCount(itemId) >= count;
    }
}
