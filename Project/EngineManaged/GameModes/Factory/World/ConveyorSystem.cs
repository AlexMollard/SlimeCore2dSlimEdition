using System;
using System.Collections.Generic;
using EngineManaged.Numeric;
using SlimeCore.Source.Core;
using SlimeCore.Source.World.Actors;

namespace SlimeCore.GameModes.Factory.World;

public class ConveyorSystem : IDisposable
{
    private ConveyorMap _map;
    private int _width;
    private int _height;
    
    // Store logical state in C# for now, or mirror it
    private struct ConveyorData
    {
        public int Tier;
        public Direction Direction;
        public bool Active;
    }
    
    private ConveyorData[] _grid;

    public ConveyorSystem(int width, int height, float tileSize)
    {
        _width = width;
        _height = height;
        _map = new ConveyorMap(width, height, tileSize);
        _grid = new ConveyorData[width * height];
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _map.Dispose();
        }
    }

    ~ConveyorSystem()
    {
        Dispose(false);
    }

    public Direction? GetConveyorDirection(int x, int y)
    {
        if (!IsValid(x, y)) return null;
        int idx = y * _width + x;
        if (!_grid[idx].Active) return null;
        return _grid[idx].Direction;
    }

    public void Update(float dt, BuildingSystem buildingSystem)
    {
        // Conveyor logic is now handled by FactoryPhysics and DroppedItem actors
        // This system only manages the grid state and rendering of the belts themselves
    }

    public void PlaceConveyor(int x, int y, int tier, Direction direction)
    {
        if (!IsValid(x, y)) return;
        
        int idx = y * _width + x;
        _grid[idx] = new ConveyorData { Tier = tier, Direction = direction, Active = true };
        
        UpdateConveyor(x, y);
        UpdateNeighbors(x, y);
    }

    public void RemoveConveyor(int x, int y)
    {
        if (!IsValid(x, y)) return;
        
        int idx = y * _width + x;
        if (_grid[idx].Active)
        {
            _grid[idx].Active = false;
            _map.RemoveConveyor(x, y);
            UpdateNeighbors(x, y);
            _map.UpdateMesh();
        }
    }

    public void Render(float time)
    {
        _map.Render(time);
    }

    private bool IsValid(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }

    private void UpdateNeighbors(int x, int y)
    {
        UpdateConveyor(x, y + 1);
        UpdateConveyor(x + 1, y);
        UpdateConveyor(x, y - 1);
        UpdateConveyor(x - 1, y);
        _map.UpdateMesh();
    }

    private void UpdateConveyor(int x, int y)
    {
        if (!IsValid(x, y)) return;
        
        int idx = y * _width + x;
        var data = _grid[idx];
        
        if (!data.Active) return;
        
        _map.SetConveyor(x, y, data.Tier, data.Direction);
    }
}

