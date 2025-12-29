using SlimeCore.GameModes.Snake.World;

namespace SlimeCore.GameModes.Snake;

public class SnakeSettings
{
    /// <summary>
    /// Initial zoom level for the snake game, AKA cell size
    /// </summary>
    public float InitialZoom { get; set; } = 0.4f;
    /// <summary>
    /// Height of the snake world maps in cells
    /// </summary>
    public int WorldHeight { get; set; } = 240;
    /// <summary>
    /// Width of the snake world maps in cells
    /// </summary>
    public int WorldWidth { get; set; } = 240;
    /// <summary>
    /// Base Terrain on world generation
    /// </summary>
    public SnakeTerrain BaseTerrain { get; set; } = SnakeTerrain.Grass;
    /// <summary>
    /// Specifies the maximum number of actors that can be processed in a single frame.
    /// </summary>
    public int ActorSingleFrameBudget = 100;
}
