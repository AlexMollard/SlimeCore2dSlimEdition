using EngineManaged.Numeric;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SlimeCore.Source.World.Grid;

[Table("map_tile")]
public record Tile<TEnum>
    where TEnum : Enum
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int PositionX { get; set; }
    public int PositionY { get; set; }

    public TEnum Type { get; set; }

    public bool Blocked { get; set; }

    public bool Food { get; set; }
    [NotMapped]
    public Vec2i Position => new Vec2i(PositionX, PositionY);

    public Tile(Action<TileOptions<TEnum>> configure)
    {
        var opts = new TileOptions<TEnum>();
        configure(opts);

        Type = opts.Type;
        Blocked = opts.Blocked;
        Food = opts.Food;
    }

    public void ApplyOptions(Action<TileOptions<TEnum>> configure)
    {
        var opts = new TileOptions<TEnum>
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
}

public sealed class TileOptions<TEnum>
    where TEnum : Enum
{
    public TEnum Type { get; set; } = default!;
    public bool Blocked { get; set; }
    public bool Food { get; set; }
}
