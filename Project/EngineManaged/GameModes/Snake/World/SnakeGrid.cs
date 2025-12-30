using EngineManaged.Scene;
using SlimeCore.Source.World.Grid;
using System.ComponentModel.DataAnnotations.Schema;

namespace SlimeCore.GameModes.Snake.World;

public class SnakeGrid : GridSystem<SnakeGame, SnakeTerrain, SnakeTileOptions, SnakeTile>
{
    [NotMapped]
    public Entity[][]? GridRenders { get; set; }

    /// <summary>
    /// How large each cell is when rendered (AKA cell size in world units)
    /// </summary>
    [NotMapped]
    public float Zoom { get; set; } = 0.4f;

    public SnakeGrid(int worldWidth, int worldHeight, SnakeTerrain init_type, float zoom) : base(worldWidth, worldHeight, init_type)
    {
        Zoom = zoom;
    }

    public void Initialize(int viewWidth, int viewHeight)
    {
        GridRenders = new Entity[viewWidth][];
        for (int x = 0; x < viewWidth; x++)
        {
            GridRenders[x] = new Entity[viewHeight];
            for (int y = 0; y < viewHeight; y++)
            {
                var entity = SceneFactory.CreateQuad(0, 0, Zoom, Zoom, 1f, 1f, 1f, layer: 0);
                var transform = entity.GetComponent<TransformComponent>();
                transform.Anchor = (0.5f, 0.5f);
                transform.Layer = 0;
                GridRenders[x][y] = entity;
            }
        }
    }

    public void Destroy()
    {
        foreach (var entities in GridRenders)
        {
            foreach (var entity in entities)
            {
                entity.Destroy();
            }
        }

        GridRenders = null;
    }
}
