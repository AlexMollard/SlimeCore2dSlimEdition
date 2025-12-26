using EngineManaged.Scene;
using SlimeCore.Source.World.Grid;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.GameModes.Snake.World;

public class SnakeGrid : GridSystem<SnakeTerrain, SnakeTileOptions, SnakeTile>
{
    
    
    public SnakeGrid(int width, int height, SnakeTerrain init_type) : base(width, height, init_type)
    {
    }
}
