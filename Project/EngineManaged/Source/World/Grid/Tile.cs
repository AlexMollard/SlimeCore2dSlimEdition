using EngineManaged.Numeric;
using MessagePack;
using SlimeCore.Source.Core;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SlimeCore.Source.World.Grid;

[MessagePackObject]
public class Tile<TGameMode, TEnum, TOptions>
    where TGameMode : IGameMode
    where TEnum : Enum
    where TOptions : TileOptions<TEnum>, new()
{
    [Key(0)]
    public Guid Id { get; init; } = Guid.NewGuid();
    [Key(1)]
    public int PositionX { get; set; }
    [Key(2)]
    public int PositionY { get; set; }
    [Key(3)]
    public TEnum Type { get; set; } = default!;

    [NotMapped]
    [IgnoreMember]
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
    
}

public class TileOptions<TEnum>
    where TEnum : Enum
{
    public TEnum Type { get; set; } = default!;
}
