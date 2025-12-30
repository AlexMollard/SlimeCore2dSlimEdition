using EngineManaged.Numeric;
using EngineManaged.Scene;
using EngineManaged.UI;
using SlimeCore.GameModes.Factory;
using SlimeCore.GameModes.Factory.Actors;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.Source.Core;
using System;

namespace SlimeCore.GameModes.Factory.States;

public class StateFactoryMenu : IGameState<FactoryGame>
{
    private UIText _gameLabel;
    private UIButton? _startBtn;
    
    // Background World
    private Vec2 _cam;
    private IntPtr _tileMap;
    private ulong _cameraEntity;
    private Entity? _menuBg;
    
    // Menu Layout
    private const float MenuWidth = 15.0f;
    private const float MenuHeight = 60.0f; // Make it large enough to cover vertical
    private const float MenuOffset = -12.0f; // Offset from center to left

    public void Enter(FactoryGame game)
    {
        FactoryResources.Load();
        
        // Initialize World for background
        game.World?.Initialize(FactoryGame.MAX_VIEW_W, FactoryGame.MAX_VIEW_H);
        
        // Create Camera
        _cameraEntity = Native.Entity_Create();
        Native.Entity_AddComponent_Transform(_cameraEntity);
        Native.Entity_AddComponent_Camera(_cameraEntity);
        Native.Entity_SetPrimaryCamera(_cameraEntity, true);
        Native.Entity_SetCameraSize(_cameraEntity, FactoryGame.VIEW_H);
        
        // Generate World
        var gen = new FactoryWorldGenerator(Environment.TickCount);
        if (game.World != null)
        {
            gen.Generate(game.World);
            _tileMap = Native.TileMap_Create(game.World.Width(), game.World.Height(), 1.0f);
            for (var x = 0; x < game.World.Width(); x++)
            {
                for (var y = 0; y < game.World.Height(); y++)
                {
                    UpdateTile(game, x, y);
                }
            }
            Native.TileMap_UpdateMesh(_tileMap);
        }
        
        // Center camera initially
        if (game.World != null)
        {
            _cam = new Vec2(game.World.Width() / 2.0f, game.World.Height() / 2.0f);
        }
        
        var menuX = _cam.X + MenuOffset;
        var menuY = _cam.Y;

        // Create Menu Background (Dark Quad)
        // Position will be updated in Update
        _menuBg = SceneFactory.CreateQuad(menuX, menuY, MenuWidth, MenuHeight, 0.0f, 0.0f, 0.0f, 50);
        var bgSprite = _menuBg.GetComponent<SpriteComponent>();
        bgSprite.Alpha = 0.8f; 
        
        // Create UI - Text seems to ignore camera, so we place it relative to screen center (0,0)
        _gameLabel = UIText.Create("FACTORY\nGAME", 2, MenuOffset, 5.0f);
        _gameLabel.UseScreenSpace(false);
        _gameLabel.Color(1.0f, 1.0f, 1.0f);
        _gameLabel.Anchor(0.5f, 0.5f);
        _gameLabel.Layer(52);

        // Button: We create it at screen relative position for the text, but we'll need to move the background manually
        _startBtn = UIButton.Create("PLAY", MenuOffset, 0, 8.0f, 1.5f, 0.5f, 0.5f, 0.5f, layer: 51, fontSize: 1, useScreenSpace: false);
        _startBtn.Label.Color(1.0f, 1.0f, 1.0f);
        _startBtn.Clicked += () =>
        {
            game.ChangeState(new StateFactoryPlay());
        };
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
        if (structTex != IntPtr.Zero)
            Native.TileMap_SetTile(_tileMap, x, y, 2, structTex, 1, 1, 1, 1, 0);
        else
            Native.TileMap_SetTile(_tileMap, x, y, 2, IntPtr.Zero, 0, 0, 0, 0, 0);
    }

    public void Exit(FactoryGame game)
    {
        _gameLabel.Destroy();
        _startBtn?.Destroy();
        _menuBg?.Destroy();
        
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
    }

    public void Update(FactoryGame game, float dt)
    {
        // Pan Camera
        _cam.X += dt * 2.0f;
        if (game.World != null && _cam.X > game.World.Width() - 75) _cam.X = 75;

        if (_cameraEntity != 0)
        {
            Native.Entity_SetPosition(_cameraEntity, _cam.X, _cam.Y);
            if (game.World != null)
            {
                 Native.Entity_SetCameraZoom(_cameraEntity, 1.0f / game.World.Zoom);
            }
        }
        
        // Update UI Positions
        var menuX = _cam.X + MenuOffset;
        var menuY = _cam.Y;
        
        if (_menuBg != null)
        {
            var t = _menuBg.GetComponent<TransformComponent>();
            t.Position = (menuX, menuY);
        }
        
        // Update Button Background only
        if (_startBtn != null)
        {
            var t = _startBtn.Background.GetComponent<TransformComponent>();
            t.Position = (menuX, menuY);
            
            // Keep Label static
            _startBtn.Label.Position = (MenuOffset, 0);
        }
        
        _gameLabel.Position = (MenuOffset, 5.0f);
    }

    public void Draw(FactoryGame game)
    {
        if (_tileMap != IntPtr.Zero)
        {
            Native.TileMap_Render(_tileMap);
        }
    }
}
