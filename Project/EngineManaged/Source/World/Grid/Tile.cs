using EngineManaged.Numeric;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SlimeCore.Source.World.Grid;

public abstract record Tile<TEnum, TOptions>
    where TEnum : Enum
    where TOptions : TileOptions<TEnum>, new()
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int PositionX { get; set; }
    public int PositionY { get; set; }

    public TEnum Type { get; set; } = default!;

    [NotMapped]
    public Vec2i Position => new(PositionX, PositionY);

    public virtual void ApplyOptions(Action<TOptions> configure)
    {
        var opts = new TOptions
        {
            Type = Type,
        };
        configure(opts);
        Type = opts.Type;
    }
    /// <summary>
    /// Converts the terrain type to a color palette.
    /// </summary>
    /// <param name="terrain">The enum to parse</param>
    /// <param name="extraArgs">Additional args like Delta Time etc</param>
    /// <returns></returns>
    public abstract Vec3 GetPalette(params object[] extraArgs);
}

public class TileOptions<TEnum>
    where TEnum : Enum
{
    public TEnum Type { get; set; } = default!;
}
