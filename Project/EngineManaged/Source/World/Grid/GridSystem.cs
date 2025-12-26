using EngineManaged.Numeric;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace SlimeCore.Source.World.Grid;

[Table("map_reference")]
public class GridSystem<TEnum>
    where TEnum : Enum
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = "Default";

    [NotMapped]
    public ConcurrentDictionary<Vec2i, Tile<TEnum>> Grid { get; set; }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="width">the 'x' axis</param>
    /// <param name="height">the 'y' axis</param>
    /// <param name="init_type"></param>
    public GridSystem(int width, int height, TEnum init_type)
    {
        Grid = new ConcurrentDictionary<Vec2i, Tile<TEnum>>();
        for (int w = 0; w < width; w++)
            for (int h = 0; h < height; h++)
            {
                Grid.TryAdd(new Vec2i(w, h), new Tile<TEnum>(o => o.Type = init_type));
            }
    }

    public Tile<TEnum> this[int x, int y]
    {
        get => Grid[new Vec2i(x, y)];
        set => Grid[new Vec2i(x, y)] = value;
    }

    public Tile<TEnum> this[Vec2i position]
    {
        get => Grid[position];
        set => Grid[position] = value;
    }

    public void SetAll(Action<TileOptions<TEnum>> configure)
    {
        Grid.AsParallel().ForAll(kv =>
        {
            kv.Value.ApplyOptions(configure);
        });
    }

    public void Set(int x, int y, Action<TileOptions<TEnum>> config) => Set(new Vec2i(x, y), config);
    public void Set(Vec2i position, Action<TileOptions<TEnum>> config)
    {
        if (Grid.TryGetValue(position, out var tile))
        {
            tile.ApplyOptions(config);
        }
        else
        {
            Logger.Warn($"Position {position.X}, {position.Y} was not found");
        }
    }

    public Tile<TEnum>? Get(int x, int y)
    {
        if (!Grid.TryGetValue(new Vec2i(x, y), out var tile))
        {
            Logger.Warn($"Position {x}, {y} was not found");
        }
        return tile;
    }

    /// <summary>
    /// Fresh Calculation of the width of the grid.
    /// </summary>
    public int Width() => Grid.Keys.Max(k => k.X) + 1;
    /// <summary>
    /// Fresh Calculation of the height of the grid.
    /// </summary>
    public int Height() => Grid.Keys.Max(k => k.Y) + 1;

}