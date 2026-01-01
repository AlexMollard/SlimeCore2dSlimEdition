namespace SlimeCore.GameModes.Factory.Items;

public class ItemStack
{
    public ItemDefinition Item { get; set; }
    public int Count { get; set; }

    public ItemStack(ItemDefinition item, int count)
    {
        Item = item;
        Count = count;
    }
}
