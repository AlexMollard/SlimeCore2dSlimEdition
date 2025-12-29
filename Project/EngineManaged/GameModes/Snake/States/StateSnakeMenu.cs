using EngineManaged;
using EngineManaged.Scene;
using EngineManaged.UI;
using SlimeCore.Source.Core;
using System;
using System.Collections.Generic;

namespace SlimeCore.GameModes.Snake.States;

public class StateSnakeMenu : IGameState<SnakeGame>
{
    private Entity? _panelBg;
    private Entity? _panelBorder;
    private float panelBGWidth = 14.0f;
    private UIText _gameLabel;
    private UIText _gameLabelShadow;
    private UIButton? _startBtn;
    private UIButton? _optionsBtn;
    private float _animTimer;
    private List<Entity> _decorations = new();

    public void Enter(SnakeGame game)
    {
        // --- THEME COLORS ---
        // Retro Snake / Gameboy-ish greens
        float borderR = 0.6f, borderG = 0.7f, borderB = 0.1f; // Light olive
        float bgR = 0.1f, bgG = 0.2f, bgB = 0.1f;             // Dark green
        float textR = 0.6f, textG = 0.8f, textB = 0.2f;       // Bright green

        // Panel Background (14x10 to fit button comfortably)
        _panelBorder = SceneFactory.CreateQuad(0, -15, 14.5f, 10.5f, borderR, borderG, borderB, layer: 91);
        var borderTransform = _panelBorder.GetComponent<TransformComponent>();
        borderTransform.Anchor = (0.5f, 0.5f);

        _panelBg = SceneFactory.CreateQuad(0, -15, panelBGWidth, 10.0f, bgR, bgG, bgB, layer: 92);
        var bgTransform = _panelBg.GetComponent<TransformComponent>();
        bgTransform.Anchor = (0.5f, 0.5f);

        // Game Label Shadow
        _gameLabelShadow = UIText.Create("SNAKE", 2, 0.1f, 6.0f - 0.1f);
        _gameLabelShadow.Color(0, 0, 0);
        _gameLabelShadow.Anchor(0.5f, 0.5f);

        // Game Label
        _gameLabel = UIText.Create("SNAKE", 2, 0, 6.0f);
        _gameLabel.Color(textR, textG, textB);
        _gameLabel.Anchor(0.5f, 0.5f);

        // --- STYLED PLAY BUTTON ---
        // Vibrant Orange Background
        _startBtn = UIButton.Create("PLAY", 0, -15, 8.0f, 1.5f, 1.0f, 0.6f, 0.0f, layer: 96, fontSize: 1);

        // Set Text to White
        _startBtn.Label.Color(1.0f, 1.0f, 1.0f);

        _startBtn.Clicked += () =>
        {
            game.ChangeState(new StateSnakeOverworld());
        };

        // --- OPTIONS BUTTON ---
        // Darker Green/Blue Background
        _optionsBtn = UIButton.Create("OPTIONS", 0, -15, 8.0f, 1.5f, 0.2f, 0.4f, 0.4f, layer: 96, fontSize: 1);
        _optionsBtn.Label.Color(1.0f, 1.0f, 1.0f);

        _optionsBtn.Clicked += () =>
        {
            game.ChangeState(new StateSnakeOptions());
        };

        // Add some decorative "Snake" segments on the panel
        for (var i = 0; i < 4; i++)
        {
            // Create snake body segments
            var seg = SceneFactory.CreateQuad(0, -15, 0.8f, 0.8f, 0.4f, 0.8f, 0.4f, layer: 93);
            _decorations.Add(seg);
        }

        // Add an "Apple" decoration
        var apple = SceneFactory.CreateQuad(0, -15, 0.8f, 0.8f, 0.9f, 0.1f, 0.1f, layer: 93);
        _decorations.Add(apple);
    }

    public void Exit(SnakeGame game)
    {
        _panelBg?.Destroy();
        _panelBorder?.Destroy();
        _gameLabel.Destroy();
        _gameLabelShadow.Destroy();
        _startBtn?.Destroy();
        _optionsBtn?.Destroy();

        foreach (var d in _decorations) d.Destroy();
        _decorations.Clear();
    }

    public void Update(SnakeGame game, float dt)
    {
        _animTimer += dt;
        // 1. Animate Panel Slide Up
        var slideT = MathF.Min(1.0f, _animTimer * 1.5f);
        var panelY = Ease.Lerp(-18.0f, -1.0f, Ease.OutBack(slideT));

        var bgTransform = _panelBg.GetComponent<TransformComponent>();
        bgTransform.Position = (0, panelY);
        var borderTransform = _panelBorder.GetComponent<TransformComponent>();
        borderTransform.Position = (0, panelY);

        // Animate Buttons
        _startBtn.SetPosition(0, panelY + 0.5f);
        _optionsBtn.SetPosition(0, panelY - 2.0f);

        // Animate Decorations
        // Snake segments wiggle
        for (var i = 0; i < 4; i++)
        {
            if (i < _decorations.Count - 1) // Ensure we don't grab the apple
            {
                var xOffset = -3.0f + (i * 1.1f);
                var yOffset = 2.5f + MathF.Sin(_animTimer * 5.0f + i) * 0.2f;
                var t = _decorations[i].GetComponent<TransformComponent>();
                t.Position = (xOffset, panelY + yOffset);
            }
        }

        // Apple position
        if (_decorations.Count > 0)
        {
            var apple = _decorations[_decorations.Count - 1];
            var t = apple.GetComponent<TransformComponent>();
            t.Position = (3.0f, panelY + 2.5f);
        }
    }

    public void Draw(SnakeGame game)
    {
    }
}
