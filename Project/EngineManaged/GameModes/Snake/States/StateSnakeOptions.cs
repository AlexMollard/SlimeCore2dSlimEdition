using EngineManaged;
using EngineManaged.Scene;
using EngineManaged.UI;
using SlimeCore.GameModes.Snake.World;
using SlimeCore.Source.Core;
using System;

namespace SlimeCore.GameModes.Snake.States;

public class StateSnakeOptions : IGameState<SnakeGame>
{
    private Entity? _panelBg;
    private Entity? _panelBorder;
    private float panelBGWidth = 14.0f;
    private UIText _titleLabel;
    private UIText _titleLabelShadow;

    private UIButton? _zoomBtn;
    private UIButton? _terrainBtn;
    private UIButton? _worldSizeBtn;
    private UIButton? _backBtn;

    private float _animTimer;

    public void Enter(SnakeGame game)
    {
        // --- THEME COLORS ---
        float borderR = 0.6f, borderG = 0.7f, borderB = 0.1f; // Light olive
        float bgR = 0.1f, bgG = 0.2f, bgB = 0.1f;             // Dark green
        float textR = 0.6f, textG = 0.8f, textB = 0.2f;       // Bright green

        // Panel Background
        _panelBorder = SceneFactory.CreateQuad(0, -15, 14.5f, 10.5f, borderR, borderG, borderB, layer: 91);
        var borderTransform = _panelBorder.GetComponent<TransformComponent>();
        borderTransform.Anchor = (0.5f, 0.5f);

        _panelBg = SceneFactory.CreateQuad(0, -15, panelBGWidth, 10.0f, bgR, bgG, bgB, layer: 92);
        var bgTransform = _panelBg.GetComponent<TransformComponent>();
        bgTransform.Anchor = (0.5f, 0.5f);

        // Title Shadow
        _titleLabelShadow = UIText.Create("OPTIONS", 2, 0.1f, 6.0f - 0.1f);
        _titleLabelShadow.Color(0, 0, 0);
        _titleLabelShadow.Anchor(0.5f, 0.5f);

        // Title
        _titleLabel = UIText.Create("OPTIONS", 2, 0, 6.0f);
        _titleLabel.Color(textR, textG, textB);
        _titleLabel.Anchor(0.5f, 0.5f);

        // --- ZOOM BUTTON ---
        _zoomBtn = UIButton.Create($"Zoom: {game.Settings.InitialZoom:0.0}", 0, -15, 10.0f, 1.5f, 0.3f, 0.3f, 0.3f, layer: 96, fontSize: 1);
        _zoomBtn.Label.Color(1.0f, 1.0f, 1.0f);
        _zoomBtn.Clicked += () =>
        {
            // Toggle Zoom
            if (game.Settings.InitialZoom < 0.5f)
                game.Settings.InitialZoom = 0.8f;
            else
                game.Settings.InitialZoom = 0.4f;

            _zoomBtn.Label.Text($"Zoom: {game.Settings.InitialZoom:0.0}");
            game.InitializeGame();
        };

        // --- TERRAIN BUTTON ---
        _terrainBtn = UIButton.Create($"Terrain: {game.Settings.BaseTerrain}", 0, -15, 10.0f, 1.5f, 0.3f, 0.3f, 0.3f, layer: 96, fontSize: 1);
        _terrainBtn.Label.Color(1.0f, 1.0f, 1.0f);
        _terrainBtn.Clicked += () =>
        {
            // Cycle Terrain
            int current = (int)game.Settings.BaseTerrain;
            int next = (current + 1) % Enum.GetValues<SnakeTerrain>().Length;
            game.Settings.BaseTerrain = (SnakeTerrain)next;

            _terrainBtn.Label.Text($"Terrain: {game.Settings.BaseTerrain}");
            game.InitializeGame();
        };

        // --- WORLD SIZE BUTTON ---
        _worldSizeBtn = UIButton.Create($"Size: {game.Settings.WorldWidth}x{game.Settings.WorldHeight}", 0, -15, 10.0f, 1.5f, 0.3f, 0.3f, 0.3f, layer: 96, fontSize: 1);
        _worldSizeBtn.Label.Color(1.0f, 1.0f, 1.0f);
        _worldSizeBtn.Clicked += () =>
        {
            // Cycle Sizes
            if (game.Settings.WorldWidth == 240)
            {
                game.Settings.WorldWidth = 120;
                game.Settings.WorldHeight = 120;
            }
            else if (game.Settings.WorldWidth == 120)
            {
                game.Settings.WorldWidth = 480;
                game.Settings.WorldHeight = 480;
            }
            else
            {
                game.Settings.WorldWidth = 240;
                game.Settings.WorldHeight = 240;
            }

            _worldSizeBtn.Label.Text($"Size: {game.Settings.WorldWidth}x{game.Settings.WorldHeight}");
            game.InitializeGame();
        };

        // --- BACK BUTTON ---
        _backBtn = UIButton.Create("BACK", 0, -15, 10.0f, 1.5f, 0.8f, 0.2f, 0.2f, layer: 96, fontSize: 1);
        _backBtn.Label.Color(1.0f, 1.0f, 1.0f);
        _backBtn.Clicked += () =>
        {
            game.ChangeState(new StateSnakeMenu());
        };
    }

    public void Exit(SnakeGame game)
    {
        _panelBg?.Destroy();
        _panelBorder?.Destroy();
        _titleLabel.Destroy();
        _titleLabelShadow.Destroy();
        _zoomBtn?.Destroy();
        _terrainBtn?.Destroy();
        _worldSizeBtn?.Destroy();
        _backBtn?.Destroy();
    }

    public void Update(SnakeGame game, float dt)
    {
        _animTimer += dt;
        // 1. Animate Panel Slide Up
        float slideT = MathF.Min(1.0f, _animTimer * 1.5f);
        float panelY = Ease.Lerp(-18.0f, -1.0f, Ease.OutBack(slideT));

        var bgTransform = _panelBg.GetComponent<TransformComponent>();
        bgTransform.Position = (0, panelY);
        var borderTransform = _panelBorder.GetComponent<TransformComponent>();
        borderTransform.Position = (0, panelY);

        // Animate Buttons
        _zoomBtn.SetPosition(0, panelY + 2.5f);
        _terrainBtn.SetPosition(0, panelY + 0.5f);
        _worldSizeBtn.SetPosition(0, panelY - 1.5f);
        _backBtn.SetPosition(0, panelY - 3.5f);
    }

    public void Draw(SnakeGame game)
    {
    }
}
