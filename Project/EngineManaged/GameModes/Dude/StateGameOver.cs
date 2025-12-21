using System;
using EngineManaged.Scene;
using EngineManaged.UI;

namespace GameModes.Dude
{
	public class StateGameOver : IDudeState
	{
		private UIText _gameOverText;
		private UIText _finalScoreText;
		private UIButton _retryBtn;
		private UIButton _quitBtn;
		private float _animTimer;

		public void Enter(DudeGame game)
		{
			_animTimer = 0;
			game.ShakeAmount = 1.5f;

			// Explosion
			game.SpawnExplosion(game.DudePos, 30, 0.2f, 1.0f, 0.2f);
			game.SpawnExplosion(game.DudePos, 15, 1.0f, 0.0f, 0.0f);

			// Hide Game UI
			game.Dude.Destroy(); // Actually destroy/hide player
			game.ScoreText.SetVisible(false);
			game.LevelText.SetVisible(false);
			game.XPBarBg.IsVisible = false;
			game.XPBarFill.IsVisible = false;

			// Show Death UI
			game.DarkOverlay.SetColor(0.2f, 0, 0);
			game.DarkOverlay.IsVisible = true;
			game.Bg.SetColor(0, 0, 0);

			_gameOverText = UIText.Create("WASTED", 120, 0, 3);
			_gameOverText.SetColor(1, 0, 0);
			_gameOverText.SetAnchor(0.5f, 0.5f);

			_finalScoreText = UIText.Create($"FINAL VIBE: {(int)game.Score}", 42, 0, 0.5f);
			_finalScoreText.SetColor(1, 1, 1);
			_finalScoreText.SetAnchor(0.5f, 0.5f);

			_retryBtn = UIButton.Create("AGAIN", 0, -2.5f, 6, 2, 0.2f, 0.8f, 0.2f);
			_retryBtn.Clicked += () => game.Init(); // Restart

			_quitBtn = UIButton.Create("MENU", 0, -5.5f, 6, 2, 0.8f, 0.2f, 0.2f);
			_quitBtn.Clicked += () => GameManager.LoadMode(new GameModes.Snake.SnakeGame());
		}

		public void Exit(DudeGame game)
		{
			_gameOverText.Destroy();
			_finalScoreText.Destroy();
			_retryBtn.Destroy();
			_quitBtn.Destroy();

			// Clean up entities now so Init can start fresh
			foreach (var h in game.Haters) h.Ent.Destroy(); game.Haters.Clear();
			foreach (var c in game.Collectables) c.Ent.Destroy(); game.Collectables.Clear();
			foreach (var g in game.Gems) g.Ent.Destroy(); game.Gems.Clear();
			foreach (var t in game.Trails) t.Ent.Destroy(); game.Trails.Clear();
			foreach (var p in game.Particles) p.Ent.Destroy(); game.Particles.Clear();
		}

		public void Update(DudeGame game, float dt)
		{
			_animTimer += dt;

			// Animate Text Slam
			float t = MathF.Min(1.0f, _animTimer * 1.0f);
			float scale = 120.0f;
			if (t < 1.0f) scale = 300.0f - (180.0f * t);

			float shakeX = (float)(game.Rng.NextDouble() - 0.5f) * 4.0f;
			float shakeY = (float)(game.Rng.NextDouble() - 0.5f) * 4.0f;
			_gameOverText.SetPosition(shakeX, 3 + shakeY);

			// Keep updating particles so explosion finishes
			// We have to call the logic manually or expose UpdateParticles from StatePlaying
			// For now, let's just duplicate the particle update logic here or make it a public static helper
			// Simple inline update:
			for (int i = game.Particles.Count - 1; i >= 0; i--)
			{
				var p = game.Particles[i];
				p.Life -= dt * 1.5f;
				if (p.Life <= 0) { p.Ent.Destroy(); game.Particles.RemoveAt(i); }
				else
				{
					p.Pos += p.Vel * dt; p.Vel *= 0.95f; p.Ent.SetPosition(p.Pos.X, p.Pos.Y);
					float s = p.InitSize * p.Life; p.Ent.SetSize(s, s);
				}
			}
		}
	}
}