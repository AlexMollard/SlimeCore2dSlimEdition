using EngineManaged.Numeric;
using EngineManaged.Scene;
using SlimeCore.GameModes.Factory.Actors;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.Source.Core;
using SlimeCore.Source.Input;
using SlimeCore.Source.World.Actors;
using System;

namespace SlimeCore.GameModes.Factory.States;

public class StateFactoryPlay : IGameState<FactoryGame>, IDisposable
{
    private Vec2 _cam;
    private Player? _player;
    private IntPtr _tileMap;
    private ConveyorSystem? _conveyorSystem;
    private float _time;
    private ulong _cameraEntity;
    private ulong _cursorEntity;
    private int _lastGridX = -1;
    private int _lastGridY = -1;
    private bool _wasMouseDown;
    private int _currentTier = 1;

    public void Enter(FactoryGame game)
    {
        FactoryResources.Load();
        game.World?.Initialize(FactoryGame.MAX_VIEW_W, FactoryGame.MAX_VIEW_H);

        // Create Camera
        _cameraEntity = Native.Entity_Create();
        Native.Entity_AddComponent_Transform(_cameraEntity);
        Native.Entity_AddComponent_Camera(_cameraEntity);
        Native.Entity_SetPrimaryCamera(_cameraEntity, true);
        Native.Entity_SetCameraSize(_cameraEntity, FactoryGame.VIEW_H);

        // Create Cursor
        _cursorEntity = Native.Entity_Create();
        Native.Entity_AddComponent_Transform(_cursorEntity);
        Native.Entity_AddComponent_Sprite(_cursorEntity);
        Native.Entity_SetColor(_cursorEntity, 1.0f, 1.0f, 0.0f); // Yellow highlight
        Native.Entity_SetAlpha(_cursorEntity, 0.4f);
        Native.Entity_SetSize(_cursorEntity, 1.0f, 1.0f);
        Native.Entity_SetLayer(_cursorEntity, 100); // Ensure it's on top

        // Generate World
        var gen = new FactoryWorldGenerator(game.Rng?.Next() ?? 0);
        if (game.World != null)
        {
            gen.Generate(game.World);

            // Create TileMap
            _tileMap = Native.TileMap_Create(game.World.Width(), game.World.Height(), 1.0f);
            
            // Create ConveyorSystem
            _conveyorSystem = new ConveyorSystem(game.World.Width(), game.World.Height(), 1.0f);

            // Populate TileMap
            for (var x = 0; x < game.World.Width(); x++)
            {
                for (var y = 0; y < game.World.Height(); y++)
                {
                    UpdateTile(game, x, y);
                }
            }
            Native.TileMap_UpdateMesh(_tileMap);
        }

        // Center camera on world
        _cam = new Vec2(game.World.Width() / 2.0f, game.World.Height() / 2.0f);

        // Create player at center
        _player = new Player(_cam);
        game.ActorManager?.Register(_player);
        for (var i = 0; i < 5; i++)
        {
            var coords = game.Rng.Next(10) > 5 ?
               new Vec2(_cam.X + i, _cam.Y - i) :
               new Vec2(_cam.X + i, _cam.Y - i);

            game.ActorManager.Register(new Sheep(coords));
        }
    }

    private void UpdateTile(FactoryGame game, int x, int y)
    {
        if (_tileMap == IntPtr.Zero || game.World == null) return;

        var tile = game.World[x, y];

        // Layer 0: Terrain
        Native.TileMap_SetTile(_tileMap, x, y, 0, FactoryResources.GetTerrainTexture(tile.Type), 1, 1, 1, 1, 0);

        // Layer 1: Ore
        var oreTex = FactoryResources.GetOreTexture(tile.OreType);
        if (oreTex != IntPtr.Zero)
            Native.TileMap_SetTile(_tileMap, x, y, 1, oreTex, 1, 1, 1, 1, 0);
        else
            Native.TileMap_SetTile(_tileMap, x, y, 1, IntPtr.Zero, 0, 0, 0, 0, 0);

        // Layer 2: Structure
        var structTex = FactoryResources.GetStructureTexture(tile.Structure);
        var rotation = 0.0f;
        
        if (tile.Structure == FactoryStructure.ConveyorBelt)
        {
            // Use ConveyorSystem
            _conveyorSystem.PlaceConveyor(x, y, tile.Tier, tile.Direction);
            
            // Don't render in TileMap
            Native.TileMap_SetTile(_tileMap, x, y, 2, IntPtr.Zero, 0, 0, 0, 0, 0);
        }
        else
        {
            // Remove from ConveyorSystem if it was there
            _conveyorSystem.RemoveConveyor(x, y);
            
            if (structTex != IntPtr.Zero)
                Native.TileMap_SetTile(_tileMap, x, y, 2, structTex, 1, 1, 1, 1, rotation);
            else
                Native.TileMap_SetTile(_tileMap, x, y, 2, IntPtr.Zero, 0, 0, 0, 0, 0);
        }
    }

    public void Exit(FactoryGame game)
    {
        Dispose();

        if (_tileMap != IntPtr.Zero)
        {
            Native.TileMap_Destroy(_tileMap);
            _tileMap = IntPtr.Zero;
        }

        if (_cameraEntity != 0)
        {
            Native.Entity_Destroy(_cameraEntity);
            _cameraEntity = 0;
        }

        if (_cursorEntity != 0)
        {
            Native.Entity_Destroy(_cursorEntity);
            _cursorEntity = 0;
        }

        game.World?.Destroy();
        game.ActorManager?.Destroy();
    }

    public void Update(FactoryGame game, float dt)
    {
        _time += dt;
        game.ActorManager?.Tick(game, dt);
        
        // Input for Tier Selection
        if (Input.GetKeyReleased(Keycode.KEY_1)) _currentTier = 1;
        if (Input.GetKeyReleased(Keycode.KEY_2)) _currentTier = 2;
        if (Input.GetKeyReleased(Keycode.KEY_3)) _currentTier = 3;

        // Camera follows player
        if (_player != null)
        {
            _cam = Vec2.Lerp(_cam, _player.Position, dt * 5.0f);
        }

        // Update Camera Entity
        if (_cameraEntity != 0)
        {
            Native.Entity_SetPosition(_cameraEntity, _cam.X, _cam.Y);
            Native.Entity_SetCameraZoom(_cameraEntity, 1.0f / game.World.Zoom);
        }

        HandleMouse(game);
    }

    public void Draw(FactoryGame game)
    {
        Render(game);
    }

    private void HandleMouse(FactoryGame game)
    {
        // Zoom
        var scroll = Input.GetScroll();
        if (scroll != 0)
        {
            game.World.Zoom += scroll * 0.05f;
            game.World.Zoom = Math.Clamp(game.World.Zoom, 0.05f, 5.0f);
        }

        var (mx, my) = Input.GetMouseToWorld();
        var gridX = (int)Math.Floor(mx);
        var gridY = (int)Math.Floor(my);

        // Update Cursor Position
        if (_cursorEntity != 0)
        {
            Native.Entity_SetPosition(_cursorEntity, gridX + 0.5f, gridY + 0.5f);

            // Hide cursor if out of bounds
            var inBounds = gridX >= 0 && gridX < game.World.Width() && gridY >= 0 && gridY < game.World.Height();
            Native.Entity_SetRender(_cursorEntity, inBounds);
        }

        var isMouseDown = Input.IsMouseDown(Input.MouseButton.Left);
        
        if (isMouseDown)
        {
            if (!_wasMouseDown)
            {
                // Mouse just pressed
                _lastGridX = -1; 
                _lastGridY = -1;
            }

            if (gridX >= 0 && gridX < game.World.Width() && gridY >= 0 && gridY < game.World.Height())
            {
                var tile = game.World[gridX, gridY];
                
                // Determine direction based on drag
                var newDir = tile.Direction;
                if (tile.Structure != FactoryStructure.ConveyorBelt) newDir = Direction.North; // Default for new

                if (_lastGridX != -1 && (gridX != _lastGridX || gridY != _lastGridY))
                {
                    var dx = gridX - _lastGridX;
                    var dy = gridY - _lastGridY;
                    
                    if (Math.Abs(dx) > Math.Abs(dy))
                    {
                        if (dx > 0) newDir = Direction.East;
                        else newDir = Direction.West;
                    }
                    else
                    {
                        if (dy > 0) newDir = Direction.North;
                        else newDir = Direction.South;
                    }
                    
                    // Also update the PREVIOUS tile to point to this one if it was part of the drag
                    // This makes corners work naturally
                    if (game.World[_lastGridX, _lastGridY].Structure == FactoryStructure.ConveyorBelt)
                    {
                        game.World.Set(_lastGridX, _lastGridY, o => o.Direction = newDir);
                        UpdateTile(game, _lastGridX, _lastGridY);
                    }
                }

                // Allow overwriting or updating direction
                game.World.Set(gridX, gridY, o =>
                {
                    o.Structure = FactoryStructure.ConveyorBelt;
                    o.Direction = newDir;
                    o.Tier = _currentTier;
                });
                
                game.World.UpdateNeighbors(new Vec2i(gridX, gridY));
                UpdateTile(game, gridX, gridY);
                // Also update neighbors visuals as their connectivity might have changed
                UpdateTile(game, gridX, gridY + 1);
                UpdateTile(game, gridX + 1, gridY);
                UpdateTile(game, gridX, gridY - 1);
                UpdateTile(game, gridX - 1, gridY);

                Native.TileMap_UpdateMesh(_tileMap);
                
                // Update last grid pos if we moved
                if (gridX != _lastGridX || gridY != _lastGridY)
                {
                    _lastGridX = gridX;
                    _lastGridY = gridY;
                }
            }
        }
        
        _wasMouseDown = isMouseDown;

        // Right click to remove
        if (Input.IsMouseDown(Input.MouseButton.Right))
        {
            if (gridX >= 0 && gridX < game.World.Width() && gridY >= 0 && gridY < game.World.Height())
            {
                var tile = game.World[gridX, gridY];
                if (tile.Structure != FactoryStructure.None)
                {
                    game.World.Set(gridX, gridY, o =>
                    {
                        o.Structure = FactoryStructure.None;
                    });
                    
                    game.World.UpdateNeighbors(new Vec2i(gridX, gridY));
                    UpdateTile(game, gridX, gridY);
                    // Update neighbors visuals
                    UpdateTile(game, gridX, gridY + 1);
                    UpdateTile(game, gridX + 1, gridY);
                    UpdateTile(game, gridX, gridY - 1);
                    UpdateTile(game, gridX - 1, gridY);

                    Native.TileMap_UpdateMesh(_tileMap);
                }
            }
        }
    }

    private void Render(FactoryGame game)
    {
        if (_tileMap != IntPtr.Zero)
        {
            Native.TileMap_Render(_tileMap);
        }
        
        if (_conveyorSystem != null)
        {
            _conveyorSystem.Render(_time);
        }

        // Update player visual position
        if (_player != null)
        {
            var pTransform = _player.Entity.GetComponent<TransformComponent>();
            pTransform.Position = (_player.Position.X, _player.Position.Y);
            pTransform.Scale = (_player.Size, _player.Size);
        }
    }

    public void Dispose()
    {
        if (_conveyorSystem != null)
        {
            _conveyorSystem.Dispose();
            _conveyorSystem = null;
        }
    }
}
