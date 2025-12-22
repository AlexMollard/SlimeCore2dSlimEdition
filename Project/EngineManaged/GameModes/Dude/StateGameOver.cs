using System;
using EngineManaged.Scene;
using EngineManaged.UI;
using EngineManaged;
using EngineManaged.Numeric;

namespace GameModes.Dude
{
	public class StateGameOver : IDudeState
	{
		private UIText _gameOverText;

		// Stats Panel UI
		private Entity _panelBg;
		private Entity _panelBorder;
		private UIText _scoreLabel;
		private UIText _levelLabel;
		private UIText _timeLabel;

		private UIButton _retryBtn;
		private float _animTimer;

		public void Enter(DudeGame game)
		{
			_animTimer = 0;
			game.ShakeAmount = 2.0f;

			// 1. Cleanup Gameplay
			game.Dude.Destroy();
			game.ScoreText.SetVisible(false);
			game.LevelText.SetVisible(false);
			game.XPBarBg.IsVisible = false;
			game.XPBarFill.IsVisible = false;

			game.SpawnExplosion(game.DudePos, 50, 0.8f, 0.0f, 0.0f);
			game.SpawnExplosion(game.DudePos, 20, 1.0f, 1.0f, 1.0f);

			game.DarkOverlay.SetColor(0.2f, 0.0f, 0.0f);
			game.DarkOverlay.IsVisible = true;
			game.Bg.SetColor(0, 0, 0);

			// 2. UI Construction

			_gameOverText = UIText.Create("WASTED", 1, 0, 6.0f);
			_gameOverText.SetColor(1, 0, 0);
			_gameOverText.SetAnchor(0.5f, 0.5f);

			// Panel Background (14x10 to fit button comfortably)
			_panelBorder = SceneFactory.CreateQuad(0, -15, 14.5f, 10.5f, 0.8f, 0f, 0f, layer: 91);
			_panelBorder.SetAnchor(0.5f, 0.5f);

			_panelBg = SceneFactory.CreateQuad(0, -15, 14.0f, 10.0f, 0.1f, 0.1f, 0.1f, layer: 92);
			_panelBg.SetAnchor(0.5f, 0.5f);

			// Stats Text
			_scoreLabel = UIText.Create($"SCORE: {(int)game.Score}", 1, 0, -15);
			_scoreLabel.SetAnchor(0.5f, 0.5f);
			_scoreLabel.SetColor(1, 1, 1);
			_scoreLabel.SetLayer(95);

			_levelLabel = UIText.Create($"LEVEL REACHED: {game.Level}", 1, 0, -15);
			_levelLabel.SetAnchor(0.5f, 0.5f);
			_levelLabel.SetColor(0.2f, 1.0f, 1.0f);
			_levelLabel.SetLayer(95);

			_timeLabel = UIText.Create($"TIME ALIVE: {game.TimeAlive:F1}s", 1, 0, -15);
			_timeLabel.SetAnchor(0.5f, 0.5f);
			_timeLabel.SetColor(0.7f, 0.7f, 0.7f);
			_timeLabel.SetLayer(95);

			// --- STYLED RETRY BUTTON ---
			// Bright Cyan Background
			_retryBtn = UIButton.Create("TRY AGAIN", 0, -15, 10.0f, 2.0f, 0.0f, 0.8f, 1.0f, layer: 96, fontSize: 36);

			// Set Text to Black for contrast
			_retryBtn.Label.SetColor(0.1f, 0.1f, 0.1f);

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

			if (game.DarkOverlay.IsAlive) game.DarkOverlay.IsVisible = false;

			foreach (var h in game.Haters) h.Ent.Destroy(); game.Haters.Clear();
			foreach (var c in game.Collectables) c.Ent.Destroy(); game.Collectables.Clear();
			foreach (var g in game.Gems) g.Ent.Destroy(); game.Gems.Clear();
			foreach (var t in game.Trails) t.Ent.Destroy(); game.Trails.Clear();
			foreach (var p in game.Particles) p.Ent.Destroy(); game.Particles.Clear();
		}

		public void Update(DudeGame game, float dt)
		{
			_animTimer += dt;
			game.ShakeAmount = MathF.Max(0, game.ShakeAmount - dt);

			// 1. Animate Title (Vectorized Shake)
			Vec2 shake = new Vec2(
				(float)(game.Rng.NextDouble() - 0.5),
				(float)(game.Rng.NextDouble() - 0.5)
			) * (game.ShakeAmount * 5.0f);

			_gameOverText.SetPosition(shake.X, 6.0f + shake.Y);

			// 2. Animate Panel Slide Up
			float slideT = MathF.Min(1.0f, _animTimer * 1.5f);
			float panelY = Ease.Lerp(-18.0f, -1.0f, Ease.OutBack(slideT));

			_panelBg.SetPosition(0, panelY);
			_panelBorder.SetPosition(0, panelY);

			// Layout Elements inside Panel
			_scoreLabel.SetPosition(0, panelY + 3.0f);
			_levelLabel.SetPosition(0, panelY + 1.0f);
			_timeLabel.SetPosition(0, panelY - 0.5f);

			// Animate Button WITH the panel
			_retryBtn.SetPosition(0, panelY - 3.5f);

			// 3. Particles
			for (int i = game.Particles.Count - 1; i >= 0; i--)
			{
				var p = game.Particles[i];
				p.Life -= dt * 0.5f;
				if (p.Life <= 0) { p.Ent.Destroy(); game.Particles.RemoveAt(i); }
				else
				{
					// Vectorized Physics
					p.Pos += p.Vel * dt;
					p.Vel *= 0.9f;

					p.Ent.SetPosition(p.Pos.X, p.Pos.Y);
					float s = p.InitSize * p.Life;
					p.Ent.SetSize(s, s);
				}
			}
		}
	}
}