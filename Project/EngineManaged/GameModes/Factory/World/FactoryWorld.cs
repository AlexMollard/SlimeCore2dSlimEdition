using SlimeCore.Source.World.Grid;
using System.ComponentModel.DataAnnotations.Schema;

namespace SlimeCore.GameModes.Factory.World;

public class FactoryWorld : GridSystem<FactoryTerrain, FactoryTileOptions, FactoryTile>
{
    [NotMapped]
    public float Zoom { get; set; } = 1.0f;

    public FactoryWorld(int worldWidth, int worldHeight, FactoryTerrain init_type, float zoom) : base(worldWidth, worldHeight, init_type)
    {
        Zoom = zoom;
    }

    public void Initialize(int viewWidth, int viewHeight)
    {
        // No initialization needed for batch rendering
    }

    public void Destroy()
    {
        // No destruction needed
    }
}
