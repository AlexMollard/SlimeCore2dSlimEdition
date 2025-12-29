using EngineManaged.Numeric;
using EngineManaged.Scene;
using SlimeCore.GameModes.Factory.Actors;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.Source.Core;
using SlimeCore.Source.Input;
using System;

namespace SlimeCore.GameModes.Factory.States;

public class StateFactoryPlay : IGameState<FactoryGame>
{
    private Vec2 _cam;
    private Player? _player;

    public void Enter(FactoryGame game)
    {
        FactoryResources.Load();
        game.World?.Initialize(FactoryGame.MAX_VIEW_W, FactoryGame.MAX_VIEW_H);
        
        // Generate World
        var gen = new FactoryWorldGenerator(game.Rng?.Next() ?? 0);
        if (game.World != null)
        {
            gen.Generate(game.World);
        }

        // Center camera on world
        _cam = new Vec2(game.World.Width() / 2.0f, game.World.Height() / 2.0f);
        
        // Create player at center
        _player = new Player(_cam);
        game.ActorManager.Register(_player);
    }

    public void Exit(FactoryGame game)
    {
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

        HandleMouse(game);
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

        if (Input.GetMouseDown(Input.MouseButton.Left))
        {
            var (mx, my) = Input.GetMouseToWorld();
            
            var wx = (int)Math.Floor(mx / game.World.Zoom + _cam.X + 0.5f);
            
            var gridX = (int)Math.Round(mx / game.World.Zoom + _cam.X);
            var gridY = (int)Math.Round(my / game.World.Zoom + _cam.Y);

            if (gridX >= 0 && gridX < game.World.Width() && gridY >= 0 && gridY < game.World.Height())
            {
                game.World.Set(gridX, gridY, o =>
                {
                    o.Structure = FactoryStructure.ConveyorBelt;
                });
            }
        }
        
        // Right click to remove
        if (Input.GetMouseDown(Input.MouseButton.Right))
        {
            var (mx, my) = Input.GetMouseToWorld();
            var gridX = (int)Math.Round(mx / game.World.Zoom + _cam.X);
            var gridY = (int)Math.Round(my / game.World.Zoom + _cam.Y);

            if (gridX >= 0 && gridX < game.World.Width() && gridY >= 0 && gridY < game.World.Height())
            {
                game.World.Set(gridX, gridY, o =>
                {
                    o.Structure = FactoryStructure.None;
                });
            }
        }
    }

    private void Render(FactoryGame game)
    {
        if (game.World == null || game.World.RenderTiles == null) return;

        var camFloor = new Vec2((float)Math.Floor(_cam.X), (float)Math.Floor(_cam.Y));
        var camFrac = _cam - camFloor;

        // Calculate dynamic view size
        var viewW = (int)(FactoryGame.VIEW_W / game.World.Zoom);
        var viewH = (int)(FactoryGame.VIEW_H / game.World.Zoom);

        // Clamp to max allocated size
        if (viewW > FactoryGame.MAX_VIEW_W) viewW = FactoryGame.MAX_VIEW_W;
        if (viewH > FactoryGame.MAX_VIEW_H) viewH = FactoryGame.MAX_VIEW_H;

        // Ensure even numbers for centering logic
        if (viewW % 2 != 0) viewW++;
        if (viewH % 2 != 0) viewH++;

        // 1. Hide tiles that are no longer in view (if view shrank)
        for (var vx = 0; vx < FactoryGame.MAX_VIEW_W; vx++)
        {
            for (var vy = 0; vy < FactoryGame.MAX_VIEW_H; vy++)
            {
                var tileRender = game.World.RenderTiles[vx][vy];
                
                if (vx >= viewW || vy >= viewH)
                {
                    if (tileRender.IsVisible)
                    {
                        tileRender.TerrainSprite.IsVisible = false;
                        tileRender.OreSprite.IsVisible = false;
                        tileRender.StructureSprite.IsVisible = false;
                        tileRender.IsVisible = false;
                    }
                    continue;
                }

                // Calculate world coordinates
                var wx = (int)camFloor.X - viewW / 2 + vx;
                var wy = (int)camFloor.Y - viewH / 2 + vy;

                var outOfBounds = wx < 0 || wx >= game.World.Width() || wy < 0 || wy >= game.World.Height();

                var px = (vx - viewW / 2f - camFrac.X) * game.World.Zoom;
                var py = (vy - viewH / 2f - camFrac.Y) * game.World.Zoom;

                // Update Transforms
                tileRender.TerrainTransform.Position = (px, py);
                tileRender.TerrainTransform.Scale = (game.World.Zoom, game.World.Zoom);
                
                tileRender.OreTransform.Position = (px, py);
                tileRender.OreTransform.Scale = (game.World.Zoom, game.World.Zoom);
                
                tileRender.StructureTransform.Position = (px, py);
                tileRender.StructureTransform.Scale = (game.World.Zoom, game.World.Zoom);

                if (outOfBounds)
                {
                    tileRender.TerrainSprite.IsVisible = true;
                    tileRender.TerrainSprite.Color = (0.1f, 0.1f, 0.1f); // Dark void
                    tileRender.TerrainSprite.TexturePtr = IntPtr.Zero;
                    
                    tileRender.OreSprite.IsVisible = false;
                    tileRender.StructureSprite.IsVisible = false;
                }
                else
                {
                    var tile = game.World[wx, wy];
                    
                    // Layer 0: Terrain
                    tileRender.TerrainSprite.IsVisible = true;
                    tileRender.TerrainSprite.Color = (1, 1, 1);
                    tileRender.TerrainSprite.TexturePtr = FactoryResources.GetTerrainTexture(tile.Type);
                    
                    // Layer 1: Ore
                    var oreTex = FactoryResources.GetOreTexture(tile.OreType);
                    if (oreTex != IntPtr.Zero)
                    {
                        tileRender.OreSprite.IsVisible = true;
                        tileRender.OreSprite.Color = (1, 1, 1);
                        tileRender.OreSprite.TexturePtr = oreTex;
                    }
                    else
                    {
                        tileRender.OreSprite.IsVisible = false;
                    }
                    
                    // Layer 2: Structure
                    var structTex = FactoryResources.GetStructureTexture(tile.Structure);
                    if (structTex != IntPtr.Zero)
                    {
                        tileRender.StructureSprite.IsVisible = true;
                        tileRender.StructureSprite.Color = (1, 1, 1);
                        tileRender.StructureSprite.TexturePtr = structTex;
                    }
                    else
                    {
                        tileRender.StructureSprite.IsVisible = false;
                    }
                }
                
                tileRender.IsVisible = true;
            }
        }
        
        // Update player visual position relative to camera
        if (_player != null)
        {
            var pTransform = _player.Entity.GetComponent<TransformComponent>();
            var screenPos = (_player.Position - _cam) * game.World.Zoom;
            pTransform.Position = (screenPos.X, screenPos.Y);
            pTransform.Scale = (_player.Size * game.World.Zoom, _player.Size * game.World.Zoom);
        }
    }
}
