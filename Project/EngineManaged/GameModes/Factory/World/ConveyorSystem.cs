using System;
using System.Collections.Generic;
using EngineManaged.Numeric;

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
        _map.Dispose();
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
        
        // Auto-connect logic
        // If we are placing a belt, check if neighbors are pointing into us or away from us.
        // For now, just respect the placed direction, but maybe adjust visuals?
        // The user asked for "automatically connect to each other".
        // Usually this means corners.
        
        // Check input from neighbors
        // If a neighbor is pointing at us, and we are not pointing at them (obviously),
        // and we are not pointing opposite to them (head-to-head),
        // then we might form a line.
        
        // Actually, Factorio belts have a direction.
        // If you place a belt, it has a direction.
        // If you place a belt next to another, and the other is pointing into this one,
        // it connects.
        
        // The visual representation handles corners.
        // My C++ implementation currently just draws a straight belt rotated.
        // To support corners, I need to pass more info to C++ or handle it there.
        
        // Let's update C++ to handle corners.
        // But first, let's just get the straight belts working.
        
        _map.SetConveyor(x, y, data.Tier, data.Direction);
    }
}
