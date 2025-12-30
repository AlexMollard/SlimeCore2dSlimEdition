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
    private List<FactoryItem> _items = new();
    private List<ulong> _itemEntities = new(); // Pool of entities for rendering items

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
        
        foreach(ulong ent in _itemEntities) Native.Entity_Destroy(ent);
        _itemEntities.Clear();
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

    public bool TryAddItem(int x, int y, FactoryItemType type)
    {
        if (!IsValid(x, y)) return false;
        int idx = y * _width + x;
        if (!_grid[idx].Active) return false;
        
        // Simple collision: don't spawn if something is at the start of this tile
        foreach(var item in _items)
        {
            if (item.CurrentTileX == x && item.CurrentTileY == y && item.Progress < 0.3f)
                return false;
        }
        
        _items.Add(new FactoryItem 
        { 
            Type = type, 
            CurrentTileX = x, 
            CurrentTileY = y, 
            Progress = 0.0f,
            FromDirection = _grid[idx].Direction
        });
        return true;
    }

    public void Update(float dt, BuildingSystem buildingSystem)
    {
        float baseSpeed = 1.0f; // Tiles per second

        for (int i = _items.Count - 1; i >= 0; i--)
        {
            var item = _items[i];
            int idx = item.CurrentTileY * _width + item.CurrentTileX;
            
            if (!_grid[idx].Active)
            {
                // Belt removed under item?
                _items.RemoveAt(i);
                continue;
            }

            float speed = baseSpeed;
            if (_grid[idx].Tier == 2) speed = 2.0f;
            if (_grid[idx].Tier == 3) speed = 4.0f;

            // Move item
            float nextProgress = item.Progress + speed * dt;
            
            // Check for next tile
            if (nextProgress >= 1.0f)
            {
                // Try to move to next tile
                var dir = _grid[idx].Direction;
                int nx = item.CurrentTileX;
                int ny = item.CurrentTileY;
                
                switch(dir)
                {
                    case Direction.North: ny++; break;
                    case Direction.East: nx++; break;
                    case Direction.South: ny--; break;
                    case Direction.West: nx--; break;
                }
                
                // Check if next tile is valid belt
                bool moved = false;
                if (IsValid(nx, ny))
                {
                    int nIdx = ny * _width + nx;
                    
                    // Check for Building (Storage)
                    if (buildingSystem.TryAcceptItem(nx, ny, item.Type))
                    {
                        // Item consumed by building
                        _items.RemoveAt(i);
                        continue;
                    }

                    if (_grid[nIdx].Active)
                    {
                        // Check collision on next tile
                        bool blocked = false;
                        foreach(var other in _items)
                        {
                            if (other.CurrentTileX == nx && other.CurrentTileY == ny && other.Progress < 0.3f)
                            {
                                blocked = true;
                                break;
                            }
                        }
                        
                        if (!blocked)
                        {
                            item.CurrentTileX = nx;
                            item.CurrentTileY = ny;
                            item.Progress = nextProgress - 1.0f;
                            item.FromDirection = dir;
                            moved = true;
                        }
                    }
                }
                
                if (!moved)
                {
                    // Blocked at end of belt
                    item.Progress = 1.0f;
                }
                else
                {
                    item.Progress = Math.Max(0.0f, item.Progress); // Clamp
                }
            }
            else
            {
                // Check collision with item ahead on SAME belt
                bool blocked = false;
                foreach(var other in _items)
                {
                    if (other.CurrentTileX == item.CurrentTileX && other.CurrentTileY == item.CurrentTileY)
                    {
                        if (other.Progress > item.Progress && other.Progress < item.Progress + 0.3f)
                        {
                            blocked = true;
                            break;
                        }
                    }
                }
                
                if (!blocked)
                {
                    item.Progress = nextProgress;
                }
            }
            
            _items[i] = item;
        }
        
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // Ensure enough entities
        while (_itemEntities.Count < _items.Count)
        {
            ulong ent = Native.Entity_Create();
            Native.Entity_AddComponent_Transform(ent);
            Native.Entity_AddComponent_Sprite(ent);
            Native.Entity_SetSize(ent, 0.4f, 0.4f);
            Native.Entity_SetLayer(ent, 5); // Above belts (Z=0.1 in shader, but here layer is int)
            _itemEntities.Add(ent);
        }
        
        // Update entities
        for (int i = 0; i < _itemEntities.Count; i++)
        {
            if (i < _items.Count)
            {
                var item = _items[i];
                ulong ent = _itemEntities[i];
                
                Native.Entity_SetRender(ent, true);
                
                // Calculate visual position based on direction
                int idx = item.CurrentTileY * _width + item.CurrentTileX;
                var currentDir = _grid[idx].Direction;
                var fromDir = item.FromDirection;
                
                float x = item.CurrentTileX;
                float y = item.CurrentTileY;
                
                float lx = 0.5f, ly = 0.5f;

                if (currentDir == fromDir)
                {
                    // Straight
                    float offset = (item.Progress - 0.5f);
                    switch(currentDir)
                    {
                        case Direction.North: ly += offset; break;
                        case Direction.East: lx += offset; break;
                        case Direction.South: ly -= offset; break;
                        case Direction.West: lx -= offset; break;
                    }
                }
                else
                {
                    // Turn Logic
                    // 1. Determine Entry Point (Pin)
                    float pinX = 0.5f, pinY = 0.5f;
                    switch (fromDir)
                    {
                        case Direction.North: pinX = 0.5f; pinY = 0.0f; break;
                        case Direction.East:  pinX = 0.0f; pinY = 0.5f; break;
                        case Direction.South: pinX = 0.5f; pinY = 1.0f; break;
                        case Direction.West:  pinX = 1.0f; pinY = 0.5f; break;
                    }

                    // 2. Determine Exit Point (Pout)
                    float poutX = 0.5f, poutY = 0.5f;
                    switch (currentDir)
                    {
                        case Direction.North: poutX = 0.5f; poutY = 1.0f; break;
                        case Direction.East:  poutX = 1.0f; poutY = 0.5f; break;
                        case Direction.South: poutX = 0.5f; poutY = 0.0f; break;
                        case Direction.West:  poutX = 0.0f; poutY = 0.5f; break;
                    }

                    // 3. Determine Pivot (Corner)
                    // If Pin and Pout are on opposite sides, it's a straight line (or invalid turn), fallback to Lerp
                    if (Math.Abs(pinX - poutX) > 0.9f || Math.Abs(pinY - poutY) > 0.9f)
                    {
                        lx = pinX + (poutX - pinX) * item.Progress;
                        ly = pinY + (poutY - pinY) * item.Progress;
                    }
                    else
                    {
                        // Corner Pivot
                        float cx = (pinX == 0.5f) ? poutX : pinX;
                        float cy = (pinY == 0.5f) ? poutY : pinY;
                        
                        // Radius is 0.5
                        float r = 0.5f;
                        
                        // Angles
                        float startAngle = (float)Math.Atan2(pinY - cy, pinX - cx);
                        float endAngle = (float)Math.Atan2(poutY - cy, poutX - cx);
                        
                        // Shortest path interpolation
                        float diff = endAngle - startAngle;
                        if (diff > Math.PI) diff -= (float)(2 * Math.PI);
                        if (diff < -Math.PI) diff += (float)(2 * Math.PI);
                        
                        float angle = startAngle + diff * item.Progress;
                        
                        lx = cx + r * (float)Math.Cos(angle);
                        ly = cy + r * (float)Math.Sin(angle);
                    }
                }
                
                Native.Entity_SetPosition(ent, x + lx, y + ly);
                
                // Set Texture based on Item Type
                nint tex = FactoryResources.GetItemTexture(item.Type);
                if (tex != IntPtr.Zero)
                {
                    Native.Entity_SetTexturePtr(ent, tex);
                    Native.Entity_SetColor(ent, 1.0f, 1.0f, 1.0f);
                }
                else
                {
                    // Fallback to colors
                    Native.Entity_SetTexturePtr(ent, IntPtr.Zero);
                    switch(item.Type)
                    {
                        case FactoryItemType.IronOre: Native.Entity_SetColor(ent, 0.8f, 0.4f, 0.4f); break;
                        case FactoryItemType.CopperOre: Native.Entity_SetColor(ent, 0.8f, 0.5f, 0.2f); break;
                        case FactoryItemType.Coal: Native.Entity_SetColor(ent, 0.1f, 0.1f, 0.1f); break;
                        case FactoryItemType.GoldOre: Native.Entity_SetColor(ent, 1.0f, 0.8f, 0.2f); break;
                        case FactoryItemType.Stone: Native.Entity_SetColor(ent, 0.6f, 0.6f, 0.6f); break;
                        default: Native.Entity_SetColor(ent, 1, 1, 1); break;
                    }
                }
            }
            else
            {
                Native.Entity_SetRender(_itemEntities[i], false);
            }
        }
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
