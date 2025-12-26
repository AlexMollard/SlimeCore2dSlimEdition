using EngineManaged.Numeric;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace SlimeCore.Source.World.Grid;

[Table("map_reference")]
public class GridSystem<TEnum, TileOptions, Tile>
    where TEnum : Enum
    where TileOptions : TileOptions<TEnum>, new()
    where Tile : Tile<TEnum, TileOptions>, new()
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = "Default";

    [NotMapped]
    public ConcurrentDictionary<Vec2i, Tile> Grid { get; set; }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="width">the 'x' axis</param>
    /// <param name="height">the 'y' axis</param>
    /// <param name="init_type"></param>
    public GridSystem(int width, int height, TEnum init_type)
    {
        Grid = new ConcurrentDictionary<Vec2i, Tile>();
        for (var w = 0; w < width; w++)
            for (var h = 0; h < height; h++)
            {
                Grid.TryAdd(new Vec2i(w, h), new Tile() { 
                    PositionX = w,
                    PositionY = h,
                    Type = init_type
                });
            }
    }

    public Tile this[int x, int y]
    {
        get => Grid[new Vec2i(x, y)];
        set => Grid[new Vec2i(x, y)] = value;
    }

    public Tile this[Vec2i position]
    {
        get => Grid[position];
        set => Grid[position] = value;
    }

    public void SetAll(Action<TileOptions> configure)
    {
        Grid.AsParallel().ForAll(kv =>
        {
            kv.Value.ApplyOptions(configure);
        });
    }

    public void Set(int x, int y, Action<TileOptions> config) => Set(new Vec2i(x, y), config);
    public void Set(Vec2i position, Action<TileOptions> config)
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

    public Tile? Get(int x, int y)
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