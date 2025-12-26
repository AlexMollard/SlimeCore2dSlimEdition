using System;
using EngineManaged.Scene;
using EngineManaged.UI;
using EngineManaged;
using EngineManaged.Numeric;

namespace GameModes.Dude;

public class StateGameOver : IDudeState
{
    private UIText _gameOverText;

    // Stats Panel UI
    private Entity? _panelBg;
    private Entity? _panelBorder;
    private UIText _scoreLabel;
    private UIText _levelLabel;
    private UIText _timeLabel;

    private UIButton? _retryBtn;
    private float _animTimer;

    private float panelBGWidth = 14.0f;

    public void Enter(DudeGame game)
    {
        _animTimer = 0;
        game.ShakeAmount = 2.0f;

        // 1. Cleanup Gameplay
        game.Dude.Destroy();
        game.ScoreText.IsVisible(false);
        game.LevelText.IsVisible(false);
        game.XPBarBg.IsVisible(false);
        game.XPBarFill.IsVisible(false);

        game.SpawnExplosion(game.DudePos, 50, 0.8f, 0.0f, 0.0f);
        game.SpawnExplosion(game.DudePos, 20, 1.0f, 1.0f, 1.0f);

        var darkSprite = game.DarkOverlay.GetComponent<SpriteComponent>();
        darkSprite.Color = (0.2f, 0.0f, 0.0f);
        darkSprite.IsVisible = true;
        var bgSprite = game.Bg.GetComponent<SpriteComponent>();
        bgSprite.Color = (0, 0, 0);

        // 2. UI Construction

        _gameOverText = UIText.Create("WASTED", 1, 0, 6.0f);
        _gameOverText.Color(1, 0, 0);
        _gameOverText.Anchor(0.5f, 0.5f);

        // Panel Background (14x10 to fit button comfortably)
        _panelBorder = SceneFactory.CreateQuad(0, -15, 14.5f, 10.5f, 0.8f, 0f, 0f, layer: 91);
        var borderTransform = _panelBorder.GetComponent<TransformComponent>();
        borderTransform.Anchor = (0.5f, 0.5f);

        _panelBg = SceneFactory.CreateQuad(0, -15, panelBGWidth, 10.0f, 0.1f, 0.1f, 0.1f, layer: 92);
        var bgTransform = _panelBg.GetComponent<TransformComponent>();
        bgTransform.Anchor = (0.5f, 0.5f);

        // Stats Text - Positioned relative to panel center (0, -15)
        _scoreLabel = UIText.Create($"SCORE: {(int)game.Score}", 1, 0, -15 + 3.0f);
        _scoreLabel.Anchor(0.5f, 0.5f);
        _scoreLabel.Color(1, 1, 1);
        _scoreLabel.Layer(95);

        _levelLabel = UIText.Create($"LEVEL REACHED: {game.Level}", 1, 0, -15 + 1.0f);
        _levelLabel.Anchor(0.5f, 0.5f);
        _levelLabel.Color(0.2f, 1.0f, 1.0f);
        _levelLabel.Layer(95);

        _timeLabel = UIText.Create($"TIME ALIVE: {game.TimeAlive:F1}s", 1, 0, -15 - 0.5f);
        _timeLabel.Anchor(0.5f, 0.5f);
        _timeLabel.Color(0.7f, 0.7f, 0.7f);
        _timeLabel.Layer(95);

        // --- STYLED RETRY BUTTON ---
        // Bright Cyan Background
        _retryBtn = UIButton.Create("TRY AGAIN", 0, -15, 10.0f, 2.0f, 0.0f, 0.8f, 1.0f, layer: 96, fontSize: 1);

        // Set Text to Black for contrast
        _retryBtn.Label.Color(0.1f, 0.1f, 0.1f);

        _retryBtn.Clicked += () =>
        {
            game.Shutdown();
            game.Init();
        };
    }

    public void Exit(DudeGame game)
    {
        _gameOverText.Destroy();
        _panelBg.Destroy();
        _panelBorder.Destroy();
        _scoreLabel.Destroy();
        _levelLabel.Destroy();
        _timeLabel.Destroy();
        _retryBtn.Destroy();

        if (game.DarkOverlay.IsAlive)
        {
            var darkSprite = game.DarkOverlay.GetComponent<SpriteComponent>();
            darkSprite.IsVisible = false;
        }

        foreach (var h in game.Haters) h.Ent.Destroy(); game.Haters.Clear();
        foreach (var c in game.Collectables) c.Ent.Destroy(); game.Collectables.Clear();
        foreach (var g in game.Gems) g.Ent.Destroy(); game.Gems.Clear();
        foreach (var t in game.Trails) t.Ent.Destroy(); game.Trails.Clear();
    }
    public void Update(DudeGame game, float dt)
    {
        _animTimer += dt;
        game.ShakeAmount = MathF.Max(0, game.ShakeAmount - dt);

        // Shake Camera
        if (game.Camera.IsAlive)
        {
            var t = game.Camera.GetComponent<TransformComponent>();
            if (game.ShakeAmount > 0)
            {
                var shakeX = (float)(game.Rng.NextDouble() - 0.5) * game.ShakeAmount;
                var shakeY = (float)(game.Rng.NextDouble() - 0.5) * game.ShakeAmount;
                t.Position = (shakeX, shakeY);
            }
            else
            {
                t.Position = (0, 0);
            }
        }

        // 1. Animate Title (Vectorized Shake)
        var shake = new Vec2(
            (float)(game.Rng.NextDouble() - 0.5),
            (float)(game.Rng.NextDouble() - 0.5)
        ) * (game.ShakeAmount * 5.0f);

        _gameOverText.Position = (shake.X, 6.0f + shake.Y);

        // 2. Animate Panel Slide Up
        var slideT = MathF.Min(1.0f, _animTimer * 1.5f);
        var panelY = Ease.Lerp(-18.0f, -1.0f, Ease.OutBack(slideT));

        var bgTransform = _panelBg.GetComponent<TransformComponent>();
        bgTransform.Position = (0, panelY);
        var borderTransform = _panelBorder.GetComponent<TransformComponent>();
        borderTransform.Position = (0, panelY);

        // Layout Elements inside Panel
        _scoreLabel.Position = (0, panelY + 3.0f);
        _levelLabel.Position = (0, panelY + 1.0f);
        _timeLabel.Position = (0, panelY - 0.5f);

        // Animate Button WITH the panel
        _retryBtn.SetPosition(0, panelY - 3.5f);

    }
}