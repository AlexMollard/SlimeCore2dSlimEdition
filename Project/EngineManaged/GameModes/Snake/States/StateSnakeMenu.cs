using EngineManaged;
using EngineManaged.Scene;
using EngineManaged.UI;
using SlimeCore.Source.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.GameModes.Snake.States;

public class StateSnakeMenu : IGameState<SnakeGame>
{
    private Entity? _panelBg;
    private Entity? _panelBorder;
    private float panelBGWidth = 14.0f;
    private UIText _gameLabel;
    private UIButton? _startBtn;
    private float _animTimer;
    
    public void Enter(SnakeGame game)
    {

        // Panel Background (14x10 to fit button comfortably)
        _panelBorder = SceneFactory.CreateQuad(0, -15, 14.5f, 10.5f, 0.8f, 0f, 0f, layer: 91);
        var borderTransform = _panelBorder.GetComponent<TransformComponent>();
        borderTransform.Anchor = (0.5f, 0.5f);

        _panelBg = SceneFactory.CreateQuad(0, -15, panelBGWidth, 10.0f, 0.1f, 0.1f, 0.1f, layer: 92);
        var bgTransform = _panelBg.GetComponent<TransformComponent>();
        bgTransform.Anchor = (0.5f, 0.5f);
        
        //Game Label
        _gameLabel = UIText.Create("Snake Game", 1, 0, 6.0f);
        _gameLabel.Color(1, 0, 0);
        _gameLabel.Anchor(0.5f, 0.5f);

        // --- STYLED RETRY BUTTON ---
        // Bright Cyan Background
        _startBtn = UIButton.Create("PLAY", 0, -15, 10.0f, 2.0f, 0.0f, 0.8f, 1.0f, layer: 96, fontSize: 1);

        // Set Text to Black for contrast
        _startBtn.Label.Color(0.1f, 0.1f, 0.1f);

        _startBtn.Clicked += () =>
        {
            game.ChangeState(new StateSnakeOverworld());
        };

    }

    public void Exit(SnakeGame game)
    {
        _panelBg.Destroy();
        _panelBorder.Destroy();
        _gameLabel.Destroy();
        _startBtn.Destroy();
        _startBtn.Destroy();
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

        // Animate Button WITH the panel
        _startBtn.SetPosition(0, panelY - 3.5f);
    }
}
