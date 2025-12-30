using System;
using EngineManaged.Numeric;

namespace SlimeCore.GameModes.Factory.World;

public class ConveyorMap : IDisposable
{
    private IntPtr _nativeMap;
    private int _width;
    private int _height;
    private float _tileSize;

    public ConveyorMap(int width, int height, float tileSize)
    {
        _width = width;
        _height = height;
        _tileSize = tileSize;
        _nativeMap = Native.ConveyorMap_Create(width, height, tileSize);
    }

    public void Dispose()
    {
        if (_nativeMap != IntPtr.Zero)
        {
            Native.ConveyorMap_Destroy(_nativeMap);
            _nativeMap = IntPtr.Zero;
        }
    }

    public void SetConveyor(int x, int y, int tier, Direction direction)
    {
        // Direction: 0=N, 1=E, 2=S, 3=W
        int dir = direction switch
        {
            Direction.North => 0,
            Direction.East => 1,
            Direction.South => 2,
            Direction.West => 3,
            _ => 0
        };
        
        Native.ConveyorMap_SetConveyor(_nativeMap, x, y, tier, dir);
    }

    public void RemoveConveyor(int x, int y)
    {
        Native.ConveyorMap_RemoveConveyor(_nativeMap, x, y);
    }

    public void UpdateMesh()
    {
        Native.ConveyorMap_UpdateMesh(_nativeMap);
    }

    public void Render(float time)
    {
        Native.ConveyorMap_Render(_nativeMap, time);
    }
}
