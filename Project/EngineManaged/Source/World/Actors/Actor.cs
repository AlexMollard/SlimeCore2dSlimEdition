using EngineManaged.Numeric;
using SlimeCore.Source.Core;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Intrinsics.X86;

namespace SlimeCore.Source.World.Actors;
/// <summary>
/// Saveable base class for all actors in the world
/// </summary>
[Table("actors")]
public abstract class Actor<TEnum, TGameMode>
    where TEnum : Enum
    where TGameMode : IGameMode
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public abstract TEnum Kind { get; }

    protected float ActionCooldown;

    /// <summary>
    /// Grid Unbound Position
    /// </summary>
    [NotMapped]
    public Vec2 Position { get; set; }

    public Guid? MapReference { get; set; }

    public Guid? TileReference { get; set; }
    /// <summary>
    /// A background processor of the actor that takes actions based on the game mode;
    /// Avoid using delta time for long lived low priority actors 
    /// </summary>
    /// <param name="mode"></param>
    /// <param name="deltaTime"></param>
    /// <returns>Whether another action should be taken by this entity</returns>
    public abstract bool TakeAction(TGameMode mode, float deltaTime);

    public abstract bool Tick(TGameMode mode, float deltaTime);

    public abstract void Destroy();

    protected static int ToPriority(TEnum value)
        => Convert.ToInt32(value);

    protected abstract float ActionInterval { get; }

    public virtual int Priority => ToPriority(Kind);
}
