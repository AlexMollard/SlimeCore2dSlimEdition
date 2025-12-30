using EngineManaged.Numeric;
using System;

namespace SlimeCore.GameModes.Factory.World;

public enum Direction
{
    North,
    East,
    South,
    West
}

public enum TileShape
{
    None,
    Straight,
    CornerLeft,
    CornerRight,
    TJunction,
    Cross,
    End
}

[Flags]
public enum ConnectivityMask
{
    None = 0,
    North = 1 << 0,
    East = 1 << 1,
    South = 1 << 2,
    West = 1 << 3,
    NorthEast = 1 << 4,
    SouthEast = 1 << 5,
    SouthWest = 1 << 6,
    NorthWest = 1 << 7
}

public static class ConnectivityHelpers
{
    public static Vec2i GetOffset(this Direction dir)
    {
        return dir switch
        {
            Direction.North => new Vec2i(0, 1),
            Direction.East => new Vec2i(1, 0),
            Direction.South => new Vec2i(0, -1),
            Direction.West => new Vec2i(-1, 0),
            _ => new Vec2i(0, 0)
        };
    }

    public static Direction Opposite(this Direction dir)
    {
        return dir switch
        {
            Direction.North => Direction.South,
            Direction.East => Direction.West,
            Direction.South => Direction.North,
            Direction.West => Direction.East,
            _ => dir
        };
    }

    public static Direction RotateRight(this Direction dir)
    {
        return dir switch
        {
            Direction.North => Direction.East,
            Direction.East => Direction.South,
            Direction.South => Direction.West,
            Direction.West => Direction.North,
            _ => dir
        };
    }

    public static Direction RotateLeft(this Direction dir)
    {
        return dir switch
        {
            Direction.North => Direction.West,
            Direction.West => Direction.South,
            Direction.South => Direction.East,
            Direction.East => Direction.North,
            _ => dir
        };
    }
}
