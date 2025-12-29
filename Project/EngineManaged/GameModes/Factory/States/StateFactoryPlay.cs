using EngineManaged.Numeric;
using EngineManaged.Scene;
using SlimeCore.GameModes.Factory.Actors;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.Source.Core;
using System;

namespace SlimeCore.GameModes.Factory.States;

public class StateFactoryPlay : IGameState<FactoryGame>
{
    private Vec2 _cam;
    private Player? _player;
    private IntPtr _tileMap;
    private ulong _cameraEntity;
    private ulong _cursorEntity;

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
    }

    private void UpdateTile(FactoryGame game, int x, int y)
    {
        if (_tileMap == IntPtr.Zero || game.World == null) return;

        var tile = game.World[x, y];

        // Layer 0: Terrain
        Native.TileMap_SetTile(_tileMap, x, y, 0, FactoryResources.GetTerrainTexture(tile.Type), 1, 1, 1, 1);

        // Layer 1: Ore
        var oreTex = FactoryResources.GetOreTexture(tile.OreType);
        if (oreTex != IntPtr.Zero)
            Native.TileMap_SetTile(_tileMap, x, y, 1, oreTex, 1, 1, 1, 1);
        else
            Native.TileMap_SetTile(_tileMap, x, y, 1, IntPtr.Zero, 0, 0, 0, 0);

        // Layer 2: Structure
        var structTex = FactoryResources.GetStructureTexture(tile.Structure);
        if (structTex != IntPtr.Zero)
            Native.TileMap_SetTile(_tileMap, x, y, 2, structTex, 1, 1, 1, 1);
        else
            Native.TileMap_SetTile(_tileMap, x, y, 2, IntPtr.Zero, 0, 0, 0, 0);
    }

    public void Exit(FactoryGame game)
    {
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
        game.ActorManager?.Tick(game, dt);
        
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

        if (Input.IsMouseDown(Input.MouseButton.Left))
        {
            if (gridX >= 0 && gridX < game.World.Width() && gridY >= 0 && gridY < game.World.Height())
            {
                game.World.Set(gridX, gridY, o =>
                {
                    o.Structure = FactoryStructure.ConveyorBelt;
                });
                UpdateTile(game, gridX, gridY);
                Native.TileMap_UpdateMesh(_tileMap);
            }
            SafeNativeMethods.Engine_Log("Holding left click");
        }

        // Right click to remove
        if (Input.IsMouseDown(Input.MouseButton.Right))
        {
            if (gridX >= 0 && gridX < game.World.Width() && gridY >= 0 && gridY < game.World.Height())
            {
                game.World.Set(gridX, gridY, o =>
                {
                    o.Structure = FactoryStructure.None;
                });
                UpdateTile(game, gridX, gridY);
                Native.TileMap_UpdateMesh(_tileMap);
            }
        }
    }

    private void Render(FactoryGame game)
    {
        if (_tileMap != IntPtr.Zero)
        {
            Native.TileMap_Render(_tileMap);
        }

        // Update player visual position
        if (_player != null)
        {
            var pTransform = _player.Entity.GetComponent<TransformComponent>();
            pTransform.Position = (_player.Position.X, _player.Position.Y);
            pTransform.Scale = (_player.Size, _player.Size);
        }
    }
}
