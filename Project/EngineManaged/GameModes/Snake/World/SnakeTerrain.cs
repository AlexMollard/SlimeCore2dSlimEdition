using EngineManaged.Numeric;
using SlimeCore.Source.World.Grid;
using System;

namespace SlimeCore.GameModes.Snake.World;

public enum SnakeTerrain : int
{
    Grass,
    Rock,
    Water,
    Lava,
    Mud,
    Ice,
    Speed
}

public class SnakeTile : Tile<SnakeTerrain, SnakeTileOptions>
{
    /// <summary>
    /// Is this a solid tile that cannot be traversed?
    /// </summary>
    public bool Blocked { get; set; }
    /// <summary>
    /// Is this tile containing food for the snake?
    /// </summary>
    public bool Food { get; set; }

    public SnakeTile()
    {
    }

    public SnakeTile(Action<TileOptions<SnakeTerrain>> configure)
    {
        ApplyOptions(configure);
    }

    public override void ApplyOptions(Action<SnakeTileOptions> configure)
    {
        var opts = new SnakeTileOptions
        {
            Type = Type,
            Blocked = Blocked,
            Food = Food
        };

        configure(opts);

        Type = opts.Type;
        Blocked = opts.Blocked;
        Food = opts.Food;
    }
    public override Vec3 GetPalette(params object[] extraArgs)
    {
        var isAlt = (PositionX + PositionY) % 2 == 0;
        return Type switch
        {
            SnakeTerrain.Rock => Palette.COL_ROCK,
            SnakeTerrain.Water => Palette.COL_WATER,
            SnakeTerrain.Lava => Palette.COL_LAVA,
            SnakeTerrain.Ice => isAlt ? Palette.COL_ICE_1 : Palette.COL_ICE_2,
            SnakeTerrain.Mud => isAlt ? Palette.COL_MUD_1 : Palette.COL_MUD_2,
            SnakeTerrain.Speed => (isAlt ? Palette.COL_SPEED_1 : Palette.COL_SPEED_2) * (0.8f + 0.2f * (float)Math.Sin((float)extraArgs[0] * 15f)),
            _ => isAlt ? Palette.COL_GRASS_1 : Palette.COL_GRASS_2
        };
    }

    // --- Palette ---
    public static class Palette
    {
        public static Vec3 COL_GRASS_1 { get; } = new(0.05f, 0.05f, 0.12f);
        public static Vec3 COL_GRASS_2 { get; } = new(0.03f, 0.03f, 0.10f);
        public static Vec3 COL_ROCK { get; } = new(0.20f, 0.20f, 0.35f);
        public static Vec3 COL_WATER { get; } = new(0.10f, 0.60f, 0.80f);
        public static Vec3 COL_LAVA { get; } = new(1.00f, 0.20f, 0.00f);
        public static Vec3 COL_MUD_1 { get; } = new(0.12f, 0.06f, 0.06f);
        public static Vec3 COL_MUD_2 { get; } = new(0.08f, 0.04f, 0.04f);
        public static Vec3 COL_ICE_1 { get; } = new(0.60f, 0.90f, 1.00f);
        public static Vec3 COL_ICE_2 { get; } = new(0.45f, 0.75f, 0.95f);
        public static Vec3 COL_SPEED_1 { get; } = new(0.80f, 0.80f, 0.40f);
        public static Vec3 COL_SPEED_2 { get; } = new(0.60f, 0.60f, 0.30f);
        public static Vec3 COL_TINT_ICE { get; } = new(0.80f, 1.00f, 1.00f);
        public static Vec3 COL_TINT_MUD { get; } = new(0.30f, 0.20f, 0.15f);
        public static Vec3 COL_FOOD_APPLE { get; } = new(1.00f, 0.00f, 0.90f);
        public static Vec3 COL_FOOD_GOLD { get; } = new(1.00f, 0.85f, 0.00f);
        public static Vec3 COL_FOOD_PLUM { get; } = new(0.60f, 0.20f, 0.90f);
        public static Vec3 COL_FOOD_CHILI { get; } = new(1.00f, 0.20f, 0.00f);
    }
}

public sealed class SnakeTileOptions : TileOptions<SnakeTerrain>
{
    public bool Blocked { get; set; }
    public bool Food { get; set; }
}


