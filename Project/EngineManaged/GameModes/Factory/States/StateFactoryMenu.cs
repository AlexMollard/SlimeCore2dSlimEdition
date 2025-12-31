using EngineManaged.Numeric;
using EngineManaged.Scene;
using EngineManaged.UI;
using SlimeCore.GameModes.Factory;
using SlimeCore.GameModes.Factory.Actors;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.Source.Core;
using System;
using System.Collections.Generic;

namespace SlimeCore.GameModes.Factory.States;

public class StateFactoryMenu : IGameState<FactoryGame>
{
    private UIText _gameLabel;
    private UIButton? _startBtn;
    private UIButton? _settingsBtn;
    private UIButton? _exitBtn;

    // Decorations
    private List<Entity> _decorations = new();
    private Random _rng = new();
    
    // Settings UI
    private bool _showSettings;
    private Entity? _settingsBg;
    private UIText? _settingsLabel;
    private UISlider? _zoomSlider;
    private UIText? _zoomValueLabel;
    
    // Background World
    private Vec2 _cam;
    private IntPtr _tileMap;
    private ulong _cameraEntity;
    private Entity? _menuBg;
    
    // Menu Layout
    private const float MenuWidth = 17.0f;
    private const float MenuHeight = 60.0f; // Make it large enough to cover vertical
    private const float MenuOffset = -15.0f; // Offset from center to left
    private const float SettingsOffset = 12.0f; // Offset for settings panel

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
            game.World.CalculateAllBitmasks();
            _tileMap = Native.TileMap_Create(game.World.Width(), game.World.Height(), 1.0f);
            for (int x = 0; x < game.World.Width(); x++)
            {
                for (int y = 0; y < game.World.Height(); y++)
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

        float menuX = _cam.X + MenuOffset;
        float menuY = _cam.Y;

        // Create Menu Background (Dark Quad)
        // Position will be updated in Update
        _menuBg = SceneFactory.CreateQuad(menuX, menuY, MenuWidth, MenuHeight, 0.0f, 0.0f, 0.0f, 50);
        var bgSprite = _menuBg.GetComponent<SpriteComponent>();
        bgSprite.Alpha = 0.8f; 
        
        // Create UI - Text seems to ignore camera, so we place it relative to screen center (0,0)
        _gameLabel = UIText.Create("FACTORY\nGAME", 2, MenuOffset, 8.0f);
        _gameLabel.UseScreenSpace(false);
        _gameLabel.Color(1.0f, 1.0f, 1.0f);
        _gameLabel.Anchor(0.5f, 0.5f);
        _gameLabel.Layer(52);

        // Buttons
        _startBtn = UIButton.Create("PLAY", MenuOffset, 2.0f, MenuWidth, 1.5f, 0.2f, 0.6f, 0.2f, layer: 51, fontSize: 1, useScreenSpace: false);
        _startBtn.Label.Color(1.0f, 1.0f, 1.0f);
        _startBtn.Clicked += () =>
        {
            game.ChangeState(new StateFactoryPlay());
        };

        _settingsBtn = UIButton.Create("SETTINGS", MenuOffset, -0.5f, MenuWidth, 1.5f, 0.2f, 0.4f, 0.6f, layer: 51, fontSize: 1, useScreenSpace: false);
        _settingsBtn.Label.Color(1.0f, 1.0f, 1.0f);
        _settingsBtn.Clicked += () =>
        {
            ToggleSettings(game);
        };

        _exitBtn = UIButton.Create("EXIT", MenuOffset, -3.0f, MenuWidth, 1.5f, 0.6f, 0.2f, 0.2f, layer: 51, fontSize: 1, useScreenSpace: false);
        _exitBtn.Label.Color(1.0f, 1.0f, 1.0f);
        _exitBtn.Clicked += () =>
        {
            Environment.Exit(0);
        };
        
        CreateSettingsUI(game);
        SpawnDecorations(game);
    }

    private void CreateSettingsUI(FactoryGame game)
    {
        // Settings Background
        // We create it but keep it hidden initially
        // Position will be updated in Update
        
        _settingsBg = SceneFactory.CreateQuad(0, 0, MenuWidth, MenuHeight / 2, 0.0f, 0.0f, 0.0f, 50);
        var sBgSprite = _settingsBg.GetComponent<SpriteComponent>();
        sBgSprite.Alpha = 0.8f;
        sBgSprite.IsVisible = false;
        
        var settingsLabel = UIText.Create("SETTINGS", 2, SettingsOffset, 5.0f);
        settingsLabel.UseScreenSpace(false);
        settingsLabel.Color(1.0f, 1.0f, 1.0f);
        settingsLabel.Anchor(0.5f, 0.5f);
        settingsLabel.Layer(52);
        settingsLabel.IsVisible(false);
        _settingsLabel = settingsLabel;

        // Zoom Controls
        var zoomValueLabel = UIText.Create($"Zoom: {game.Settings.InitialZoom:F1}", 1, SettingsOffset, 2.0f);
        zoomValueLabel.UseScreenSpace(false);
        zoomValueLabel.Layer(52);
        zoomValueLabel.IsVisible(false);
        _zoomValueLabel = zoomValueLabel;

        // Map initial zoom to slider value (0.5 to 3.0)
        float initialSliderVal = (game.Settings.InitialZoom - 0.5f) / 2.5f;
        
        _zoomSlider = UISlider.Create(SettingsOffset, 0.0f, 8.0f, 0.5f, initialSliderVal, layer: 51, useScreenSpace: false);
        _zoomSlider.SetVisible(false);
        _zoomSlider.OnValueChanged += (val) =>
        {
            // Map slider value back to zoom
            float zoom = 0.5f + (val * 2.5f);
            game.Settings.InitialZoom = zoom;
            if (_zoomValueLabel.HasValue)
                _zoomValueLabel.Value.Text($"Zoom: {game.Settings.InitialZoom:F1}");
        };
    }

    private void ToggleSettings(FactoryGame game)
    {
        _showSettings = !_showSettings;
        
        if (_settingsBg != null)
        {
            var s = _settingsBg.GetComponent<SpriteComponent>();
            s.IsVisible = _showSettings;
        }
        
        if (_settingsLabel.HasValue) _settingsLabel.Value.IsVisible(_showSettings);
        if (_zoomValueLabel.HasValue) _zoomValueLabel.Value.IsVisible(_showSettings);
        _zoomSlider?.SetVisible(_showSettings);
    }

    private void UpdateTile(FactoryGame game, int x, int y)
    {
        if (_tileMap == IntPtr.Zero || game.World == null) return;

        var tile = game.World[x, y];

        // Layer 0: Terrain
        FactoryTile.GetTerrainUVs(tile.Bitmask, out float u0, out float v0, out float u1, out float v1);
        Native.TileMap_SetTile(_tileMap, x, y, 0, FactoryResources.GetTerrainTexture(tile.Type), u0, v0, u1, v1, 1, 1, 1, 1, 0);

        // Layer 1: Ore
        nint oreTex = FactoryResources.GetOreTexture(tile.OreType);
        if (oreTex != IntPtr.Zero)
            Native.TileMap_SetTile(_tileMap, x, y, 1, oreTex, 0, 0, 1, 1, 1, 1, 1, 1, 0);
        else
            Native.TileMap_SetTile(_tileMap, x, y, 1, IntPtr.Zero, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        // Layer 2: Structure
        nint structTex = FactoryResources.GetStructureTexture(tile.Structure);
        if (structTex != IntPtr.Zero)
            Native.TileMap_SetTile(_tileMap, x, y, 2, structTex, 0, 0, 1, 1, 1, 1, 1, 1, 0);
        else
            Native.TileMap_SetTile(_tileMap, x, y, 2, IntPtr.Zero, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    public void Exit(FactoryGame game)
    {
        foreach (var d in _decorations) d.Destroy();
        _decorations.Clear();

        _gameLabel.Destroy();
        _startBtn?.Destroy();
        _settingsBtn?.Destroy();
        _exitBtn?.Destroy();
        
        _settingsBg?.Destroy();
        if (_settingsLabel.HasValue) _settingsLabel.Value.Destroy();
        if (_zoomValueLabel.HasValue) _zoomValueLabel.Value.Destroy();
        _zoomSlider?.Destroy();
        
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
        float menuX = _cam.X + MenuOffset;
        float menuY = _cam.Y;
        
        if (_menuBg != null)
        {
            var t = _menuBg.GetComponent<TransformComponent>();
            t.Position = (menuX, menuY);
        }
        
        _gameLabel.Position = (MenuOffset, 8.0f);
        _startBtn?.SetPosition(MenuOffset, 2.0f);
        _settingsBtn?.SetPosition(MenuOffset, -0.5f);
        _exitBtn?.SetPosition(MenuOffset, -3.0f);
        
        // Update Settings UI Positions
        if (_showSettings)
        {
            float settingsX = _cam.X + SettingsOffset;
            if (_settingsBg != null)
            {
                var t = _settingsBg.GetComponent<TransformComponent>();
                t.Position = (settingsX, menuY);
            }
            
            if (_settingsLabel.HasValue)
            {
                var lbl = _settingsLabel.Value;
                lbl.Position = (SettingsOffset, 5.0f);
            }
            if (_zoomValueLabel.HasValue)
            {
                var lbl = _zoomValueLabel.Value;
                lbl.Position = (SettingsOffset, 2.0f);
            }
            _zoomSlider?.SetPosition(SettingsOffset, 0.0f);
        }
        UpdateDecorations(dt);
    }

    public void Draw(FactoryGame game)
    {
        if (_tileMap != IntPtr.Zero)
        {
            Native.TileMap_Render(_tileMap);
        }
    }

    private void SpawnDecorations(FactoryGame game)
    {
        if (game.World == null) return;
        for (int i = 0; i < 50; i++)
        {
            int x = _rng.Next(0, game.World.Width());
            int y = _rng.Next(0, game.World.Height());
            var itemType = (FactoryItemType)_rng.Next(0, 5); // 0 to 4
            nint tex = FactoryResources.GetItemTexture(itemType);
            
            if (tex != IntPtr.Zero)
            {
                var ent = SceneFactory.CreateQuad(x, y, 1.0f, 1.0f, 1, 1, 1, 40);
                Native.Entity_SetTexturePtr(ent.Id, tex);
                _decorations.Add(ent);
            }
        }
    }

    private void UpdateDecorations(float dt)
    {
        foreach (var d in _decorations)
        {
            var t = d.GetComponent<TransformComponent>();
            t.Rotation += dt * 45.0f;
        }
    }
}
