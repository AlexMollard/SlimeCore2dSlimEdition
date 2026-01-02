using EngineManaged.Numeric;
using MessagePack;
using SlimeCore.Source.Common;
using SlimeCore.Source.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace SlimeCore.Source.World.Grid;

[MessagePackObject]
public class GridSystem<TGameMode, TEnum, TileOptions, Tile>
    where TGameMode : IGameMode
    where TEnum : Enum
    where TileOptions : TileOptions<TEnum>, new()
    where Tile : Tile<TGameMode, TEnum, TileOptions>, ITile<TGameMode>, new()
{
    /// <summary>
    /// The unique identifier for this instance of a grid system.
    /// </summary>
    [Key(0)]
    public Guid Id { get; init; } = Guid.NewGuid();
    /// <summary>
    /// Configurable action budget per tick
    /// </summary>
    [IgnoreMember]
    public int TickBudget { get; set; }
    [Key(1)]
    public string Name { get; set; } = "Default";

    [IgnoreMember]
    private int? _width;
    [IgnoreMember]
    private int? _height;

    [IgnoreMember]
    private object _gridLock = new();

    [Key(2)]
    public ConcurrentDictionary<Vec2i, Tile> Grid { get; set; }

    [IgnoreMember]
    protected Queue<Vec2i> _actionQueue = new();

    /// <summary>
    /// Only Used for MessagePack Deserialization
    /// </summary>
    [Obsolete("This is only allowed for messagepack deserialisation")]
    public GridSystem()
    {
        Grid = new ConcurrentDictionary<Vec2i, Tile>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="width">the 'x' axis</param>
    /// <param name="height">the 'y' axis</param>
    /// <param name="init_type"></param>
    public GridSystem(int width, int height, TEnum init_type, int tickBudget = 2)
    {
        Debug.Assert(tickBudget == 0 || (tickBudget % 2) == 0, "Tick budget for GridSystem equated to 0 or was not divisible by 2");

        Grid = new ConcurrentDictionary<Vec2i, Tile>();
        for (int w = 0; w < width; w++)
            for (int h = 0; h < height; h++)
            {
                Grid.TryAdd(new Vec2i(w, h), new Tile()
                {
                    PositionX = w,
                    PositionY = h,
                    Type = init_type
                });
            }

        TickBudget = tickBudget;
    }
    /// <summary>
    /// World events
    /// </summary>
    /// <param name="mode"></param>
    /// <param name="deltaTime"></param>
    public virtual void Tick(TGameMode mode, float deltaTime)
    {
        int halfBudget = TickBudget / 2;
        int budget = TickBudget;
        for (int i = 0; i < halfBudget; i++)
        {
            if (_actionQueue.Count == 0)
            {
                break;
            }
            var pos = _actionQueue.Dequeue();
            if (Grid.TryGetValue(pos, out var tile))
            {
                tile.TakeAction(mode, deltaTime);
            }
            budget--;
        }

        for (int i = 0; i < budget; i++)
        {
            if (_actionQueue.Count == 0)
            {
                break;
            }
            var pos = _actionQueue.Dequeue();
            if (Grid.TryGetValue(pos, out var tile))
            {
                tile.Tick(mode, deltaTime);
            }
        }
    }
    /// <summary>
    /// Hard Full Process of the action queue.
    /// Good for loading screens or manual updates.
    /// </summary>
    /// <param name=""></param>
    /// <param name="deltaTime"></param>
    public virtual void ManualTick(TGameMode mode, float deltaTime)
    {
        while (_actionQueue.Count > 0)
        {
            var pos = _actionQueue.Dequeue();
            if (Grid.TryGetValue(pos, out var tile))
            {
                tile.TakeAction(mode, deltaTime);
            }
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

    public Tile this[Vec2 position]
    {
        get => Grid[position.ToVec2Int()];
        set => Grid[position.ToVec2Int()] = value;
    }

    public void SetAll(Action<TileOptions> configure)
    {
        Grid.AsParallel().ForAll(kv =>
        {
            kv.Value.ApplyOptions(configure);
        });
    }

    public Tile? Set(int x, int y, Action<TileOptions> config) => Set(new Vec2i(x, y), config);
    public virtual Tile? Set(Vec2i position, Action<TileOptions> config)
    {
        if (Grid.TryGetValue(position, out var tile))
        {
            tile.ApplyOptions(config);
            return tile;
        }
        else
        {
            Logger.Warn($"Position {position.X}, {position.Y} was not found");
            return null;
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
    /// Removes tiles from the grid.
    /// </summary>
    /// <param name="count"></param>
    /// <param name="axis">x (width) if true, y (height) if false</param>
    public void RemoveTiles(int count, bool axis)
    {
        //Really want this to be thread safe.
        lock (_gridLock)
        {
            if (axis)
            {
                var toRemoveWidth = Grid.Keys.Where(k => k.X >= Width() - count).ToArray();
                foreach (var Xtile in toRemoveWidth)
                {
                    Grid.Remove(Xtile, out _);
                }
                _width = null;
                _height = null;
            }
            else
            {
                var toRemoveHeight = Grid.Keys.Where(k => k.Y >= Width() - count).ToArray();
                foreach (var Ytile in toRemoveHeight)
                {
                    Grid.Remove(Ytile, out _);
                }
            }
        }
    }

    /// <summary>
    /// the width of the grid (X axis).
    /// </summary>
    public int Width()
    {
        if (_width is null)
        {
            lock (_gridLock)
            {
                _width ??= Grid.Keys.Max(k => k.X) + 1;
            }
        }
        return _width.Value;
    }
    /// <summary>
    /// the height of the grid (Y axis).
    /// </summary>
    public int Height()
    {
        if (_height is null)
        {
            lock (_gridLock)
            {
                _height ??= Grid.Keys.Max(k => k.Y) + 1;
            }
        }
        return _height.Value;
    }

    /// <summary>
    /// Enqueue a tile for action processing.
    /// </summary>
    public void Register(Vec2i position) => _actionQueue.Enqueue(position);
    /// <summary>
    /// Enqueue a tile for action processing.
    /// </summary>
    public void Register(int x, int y) => _actionQueue.Enqueue(new Vec2i(x, y));

    public void Register(Tile tile) => _actionQueue.Enqueue(tile.Position);

    public bool InBounds(int x, int y) => InBounds(new Vec2i(x, y));

    public bool InBounds(Vec2i position)
    {
        return
            position.X >= 0 &&
            position.Y >= 0 &&
            position.X < Width() &&
            position.Y < Height();
    }
}