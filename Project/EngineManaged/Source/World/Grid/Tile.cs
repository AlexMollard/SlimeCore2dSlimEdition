using EngineManaged.Numeric;
using SlimeCore.Source.Core;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SlimeCore.Source.World.Grid;

public abstract class Tile<TGameMode, TEnum, TOptions>
    where TGameMode : IGameMode
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
    /// <summary>
    /// Any background logic for the tile. e.g. spreading fire, growing crops, etc.
    /// Random Required actions
    /// </summary>
    /// <param name="mode"></param>
    /// <returns></returns>
    public abstract void Tick(TGameMode mode, float deltaTime);

    /// <summary>
    /// Any Forefront logic for the tile. e.g. renders, chain reactions, player interactions, etc.
    /// Targeted Required actions
    /// </summary>
    /// <param name="mode"></param>
    /// <returns>Does the tile need to continue to take actions</returns>
    public abstract bool TakeAction(TGameMode mode, float deltaTime);
}

public class TileOptions<TEnum>
    where TEnum : Enum
{
    public TEnum Type { get; set; } = default!;
}
