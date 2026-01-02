using System.Collections.Generic;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.Source.World.Grid;

namespace SlimeCore.GameModes.Factory.Buildings;

public class BuildingInstance
{
    public BuildingDefinition Definition { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public Direction Direction { get; set; }
    
    public List<IBuildingBehavior> Behaviors { get; set; } = new();
    
    // Shared state for behaviors (e.g. inventory)
    public Dictionary<string, int> Inventory { get; set; } = new();
    public int LastOutputIndex { get; set; }

    public BuildingInstance(BuildingDefinition def, int x, int y, Direction dir)
    {
        Definition = def;
        X = x;
        Y = y;
        Direction = dir;
    }
}
