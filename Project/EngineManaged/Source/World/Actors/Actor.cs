using EngineManaged.Numeric;
using SlimeCore.Source.World.Grid;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SlimeCore.Source.World.Actors;
/// <summary>
/// Saveable base class for all actors in the world
/// </summary>
[Table("actors")]
public record Actor<TEnum>
    where TEnum : Enum
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [NotMapped]
    public Vec2i Position { get; set; }

    public Guid? MapReference { get; set; }

    public Guid? TileReference { get; set; }

    public virtual GridSystem<TEnum>? Map { get; set; }
    public virtual Tile<TEnum>? Tile { get; set; }
}
