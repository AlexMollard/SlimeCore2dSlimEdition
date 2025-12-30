namespace SlimeCore.GameModes.Factory.World;

public enum FactoryItemType
{
    None,
    IronOre,
    CopperOre,
    Coal,
    GoldOre,
    Stone,
    Vegetable,
}

public struct FactoryItem
{
    public FactoryItemType Type;
    public float Progress; // 0.0 to 1.0 along the tile
    public int CurrentTileX;
    public int CurrentTileY;
    public Direction FromDirection; // The direction the item was moving when it entered this tile
    
    // Visual offset for smooth rendering
    public float VisualX;
    public float VisualY;
}
