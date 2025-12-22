using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.Core.Grid;

public record Tile<TEnum>
    where TEnum : Enum
{
    public TEnum Type { get; set; }

    public bool Blocked { get; set; }

    public bool Food { get; set; }

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
