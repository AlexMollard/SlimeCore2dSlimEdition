using System;
using EngineManaged;
using EngineManaged.Scene;
using EngineManaged.UI;

namespace GameModes.Dude
{
	public class StatePlaying : IDudeState
	{
		private UIText _controlsText;

		public void Enter(DudeGame game)
		{
			game.DarkOverlay.IsVisible = false;
			game.CardBgBackdrop.IsVisible = false;

			game.ScoreText.SetVisible(true);
			game.LevelText.SetVisible(true);
			game.XPBarBg.IsVisible = true;
			game.XPBarFill.IsVisible = true;

			_controlsText = UIText.Create("WASD Move | SPACE Dash", 24, 0, -8);
			_controlsText.SetAnchor(0.5f, 0.5f);
		}

		public void Exit(DudeGame game)
		{
			_controlsText.Destroy();
		}

		public void Update(DudeGame game, float dt)
		{
			// 1. EVENT HOOK: OnUpdate
			game.Events.OnUpdate?.Invoke(game, dt);

			UpdateDiscoLights(game, dt);
			UpdateShake(game, dt);
			UpdateParticles(game, dt);
			UpdateTrails(game, dt);

			game.TimeAlive += dt;
			game.Score += dt * 10;
			game.ScoreText.Text = $"{(int)game.Score}";

			UpdateXPBar(game, dt);
			UpdateTimers(game, dt);
			HandleInput(game, dt);
			MoveDude(game, dt);
			HandleHaters(game, dt);
			HandleCollectables(game, dt);
			HandleGems(game, dt);
		}

		private void UpdateXPBar(DudeGame game, float dt)
		{
			float targetWidth = (game.XP / game.XPToNextLevel) * 28.0f;
			if (targetWidth > 28) targetWidth = 28;
			var (curW, curH) = game.XPBarFill.GetSize();
			float newW = curW + (targetWidth - curW) * 5.0f * dt;
			game.XPBarFill.SetSize(newW, 0.6f);
		}

		private void UpdateTimers(DudeGame game, float dt)
		{
			if (game.DashTimer > 0) game.DashTimer -= dt;

			if (game.ChillTimer > 0)
			{
				game.ChillTimer -= dt;
				game.Bg.SetColor(0.1f, 0.3f, 0.4f);
			}

			if (game.ShieldTimer > 0)
			{
				game.ShieldTimer -= dt;
				float flash = 0.5f + 0.5f * MathF.Sin(game.TimeAlive * 25);
				game.Dude.SetColor(flash, flash, flash);
				game.Dude.IsVisible = game.ShieldTimer > 1.0f || (int)(game.TimeAlive * 15) % 2 == 0;
			}
			else
			{
				game.Dude.SetColor(0.2f, 1.0f, 0.2f);
				game.Dude.IsVisible = true;
			}
		}

		private void HandleInput(DudeGame game, float dt)
		{
			Vec2 input = Vec2.Zero;
			if (Input.GetKeyDown(Keycode.W)) input.Y += 1;
			if (Input.GetKeyDown(Keycode.S)) input.Y -= 1;
			if (Input.GetKeyDown(Keycode.A)) input.X -= 1;
			if (Input.GetKeyDown(Keycode.D)) input.X += 1;

			if (input.LengthSquared() > 0) input = input.Normalized();

			// Apply Stats: Acceleration * Multiplier
			game.DudeVel += input * game.Stats.Acceleration * game.Stats.AccelMult * dt;

			// Apply Drag
			float dragPower = MathF.Pow(game.Stats.Drag, dt * 60.0f);
			game.DudeVel *= dragPower;

			// Clamp Max Speed (Base * Multiplier)
			float maxSpd = game.Stats.MoveSpeed * game.Stats.SpeedMult;
			if (game.DudeVel.LengthSquared() > maxSpd * maxSpd)
				game.DudeVel = game.DudeVel.Normalized() * maxSpd;

			if (Input.GetKeyDown(Keycode.SPACE) && game.DashTimer <= 0)
			{
				// 2. EVENT HOOK: OnDash
				game.Events.OnDash?.Invoke(game);

				Vec2 dashDir = input.LengthSquared() > 0 ? input : new Vec2(1, 0);
				game.DudeVel += dashDir * 40.0f;
				game.DashTimer = game.Stats.DashCooldown;
				game.ShakeAmount = 0.3f;
				SpawnTrail(game, 1.0f);
			}
		}

		private void MoveDude(DudeGame game, float dt)
		{
			game.DudePos += game.DudeVel * dt;

			if (game.DudePos.X > 14) { game.DudePos.X = 14; game.DudeVel.X *= -0.8f; game.ShakeAmount += 0.1f; }
			if (game.DudePos.X < -14) { game.DudePos.X = -14; game.DudeVel.X *= -0.8f; game.ShakeAmount += 0.1f; }
			if (game.DudePos.Y > 9) { game.DudePos.Y = 9; game.DudeVel.Y *= -0.8f; game.ShakeAmount += 0.1f; }
			if (game.DudePos.Y < -9) { game.DudePos.Y = -9; game.DudeVel.Y *= -0.8f; game.ShakeAmount += 0.1f; }

			float sx = (float)(game.Rng.NextDouble() - 0.5) * game.ShakeAmount;
			float sy = (float)(game.Rng.NextDouble() - 0.5) * game.ShakeAmount;
			game.Dude.SetPosition(game.DudePos.X + sx, game.DudePos.Y + sy);

			float speed = game.DudeVel.Length();
			float stretch = 1.0f + (speed * 0.02f);
			float squash = 1.0f / stretch;

			// Apply Stats: Player Size
			float baseScale = 0.9f * game.Stats.PlayerSize;

			if (MathF.Abs(game.DudeVel.X) > MathF.Abs(game.DudeVel.Y))
				game.Dude.SetSize(stretch * baseScale, squash * baseScale);
			else
				game.Dude.SetSize(squash * baseScale, stretch * baseScale);

			if (speed > 15.0f) SpawnTrail(game, 0.4f);
		}

		private void HandleHaters(DudeGame game, float dt)
		{
			game.SpawnTimer -= dt;
			if (game.SpawnTimer <= 0)
			{
				SpawnHater(game);
				float ramp = MathF.Min(1.5f, game.TimeAlive * 0.02f);
				game.SpawnTimer = (1.2f - ramp) / (game.ChillTimer > 0 ? 0.5f : 1.0f);
				if (game.SpawnTimer < 0.2f) game.SpawnTimer = 0.2f;
			}

			float timeScale = game.ChillTimer > 0 ? 0.3f : 1.0f;

			for (int i = game.Haters.Count - 1; i >= 0; i--)
			{
				var me = game.Haters[i];
				Vec2 toPlayer = (game.DudePos - me.Pos).Normalized();
				Vec2 separation = Vec2.Zero;
				int neighbors = 0;
				float myRad = me.Type == HaterType.Chonker ? 2.5f : 1.2f;

				foreach (var other in game.Haters)
				{
					if (me == other) continue;
					Vec2 diff = me.Pos - other.Pos;
					float distSq = diff.LengthSquared();
					if (distSq < myRad * myRad && distSq > 0.001f)
					{
						float pushStrength = me.Type == HaterType.Chonker ? 8.0f : 4.0f;
						separation += diff.Normalized() * pushStrength;
						neighbors++;
					}
				}

				Vec2 force = toPlayer * 2.0f;
				if (neighbors > 0) force += separation * 1.5f;

				float speed = (me.Type == HaterType.Chonker ? 2.0f : 4.5f) * timeScale;
				me.Pos += force.Normalized() * speed * dt;

				float sx = (float)(game.Rng.NextDouble() - 0.5) * game.ShakeAmount * 0.5f;
				me.Ent.SetPosition(me.Pos.X + sx, me.Pos.Y + sx);

				float pulseSpeed = me.Type == HaterType.Chonker ? 5.0f : 15.0f;
				float baseSize = me.Type == HaterType.Chonker ? 2.2f : 0.8f;
				float pulse = baseSize + 0.1f * MathF.Sin(game.TimeAlive * pulseSpeed + i);
				me.Ent.SetSize(pulse, pulse);

				// Hitbox adjusted by Player Size
				float killDist = (me.Type == HaterType.Chonker ? 1.5f : 0.7f) * game.Stats.PlayerSize;

				if ((game.DudePos - me.Pos).LengthSquared() < killDist * killDist)
				{
					if (game.ShieldTimer > 0)
					{
						// 3. EVENT HOOK: OnEnemyKilled
						game.Events.OnEnemyKilled?.Invoke(game, me.Pos);

						SpawnGem(game, me.Pos, me.Type == HaterType.Chonker ? 50 : 10);
						me.Ent.Destroy();
						game.Haters.RemoveAt(i);
						game.Score += 100;
						game.ShakeAmount += 0.3f;
						game.SpawnExplosion(me.Pos, 10, 1f, 0f, 0f);
					}
					else
					{
						// 4. EVENT HOOK: OnDeath
						game.Events.OnDeath?.Invoke(game);
						game.ChangeState(new StateGameOver());
					}
				}
			}
		}

		private void HandleCollectables(DudeGame game, float dt)
		{
			// Use Registry + Luck Stat
			var def = ContentRegistry.GetRandomPowerup(game.Rng, game.Stats.Luck);
			if (def != null)
			{
				SpawnCollectable(game, def);
			}

			for (int i = game.Collectables.Count - 1; i >= 0; i--)
			{
				var c = game.Collectables[i];
				float distSq = (game.DudePos - c.Pos).LengthSquared();

				// Use Stat: Magnet Range
				if (distSq < game.Stats.MagnetRange * game.Stats.MagnetRange)
					c.Pos += (game.DudePos - c.Pos).Normalized() * 10.0f * dt;

				c.Ent.SetPosition(c.Pos.X, c.Pos.Y);
				c.Ent.SetSize(0.6f + 0.1f * MathF.Sin(game.TimeAlive * 8), 0.6f);

				// Use Stat: Player Size for pickup radius
				if (distSq < 1.0f * game.Stats.PlayerSize)
				{
					// 5. REGISTRY HOOK: OnPickup
					c.Definition.OnPickup(game, c.Pos);

					game.ShakeAmount += 0.1f;
					game.SpawnExplosion(c.Pos, 8, c.Definition.R, c.Definition.G, c.Definition.B);
					c.Ent.Destroy();
					game.Collectables.RemoveAt(i);

					// Always check level up after pickup (in case it gave XP)
					CheckLevelUp(game);
				}
			}
		}

		private void HandleGems(DudeGame game, float dt)
		{
			for (int i = game.Gems.Count - 1; i >= 0; i--)
			{
				var g = game.Gems[i];
				float distSq = (game.DudePos - g.Pos).LengthSquared();

				if (distSq < game.Stats.MagnetRange * game.Stats.MagnetRange)
				{
					g.Pos += (game.DudePos - g.Pos).Normalized() * 18.0f * dt;
				}
				g.Ent.SetPosition(g.Pos.X, g.Pos.Y);

				if (distSq < 0.5f * game.Stats.PlayerSize)
				{
					// Use Stat: Pickup Bonus
					game.XP += g.Value * game.Stats.PickupBonus;
					g.Ent.Destroy();
					game.Gems.RemoveAt(i);
					CheckLevelUp(game);
				}
			}
		}

		private void CheckLevelUp(DudeGame game)
		{
			if (game.XP >= game.XPToNextLevel)
			{
				game.XP -= game.XPToNextLevel;
				game.Level++;
				game.XPToNextLevel *= 1.35f;
				game.LevelText.Text = $"LVL {game.Level}";
				game.XPBarFill.SetSize(28.0f, 0.6f);
				game.ChangeState(new StateLevelUp());
			}
		}

		// --- SPAWNERS ---

		private void SpawnTrail(DudeGame game, float alphaStart)
		{
			var ghost = SceneFactory.CreateQuad(game.DudePos.X, game.DudePos.Y, 0.9f, 0.9f, 0.2f, 1.0f, 0.2f, layer: 15);
			ghost.SetAnchor(0.5f, 0.5f);
			var (w, h) = game.Dude.GetSize();
			ghost.SetSize(w, h);
			game.Trails.Add(new GhostTrail { Ent = ghost, Alpha = alphaStart, InitW = w, InitH = h });
		}

		private void SpawnHater(DudeGame game)
		{
			HaterType type = game.Rng.NextDouble() < 0.10 ? HaterType.Chonker : HaterType.Normal;
			Vec2 pos;
			if (game.Rng.Next(2) == 0) { pos.X = game.Rng.Next(2) == 0 ? -16 : 16; pos.Y = (game.Rng.NextSingle() * 20) - 10; }
			else { pos.X = (game.Rng.NextSingle() * 28) - 14; pos.Y = game.Rng.Next(2) == 0 ? -11 : 11; }
			float size = type == HaterType.Chonker ? 2.2f : 0.8f;
			float r = type == HaterType.Chonker ? 0.6f : 1.0f;
			float b = type == HaterType.Chonker ? 0.8f : 0.2f;
			var ent = SceneFactory.CreateQuad(pos.X, pos.Y, size, size, r, 0f, b, layer: 5);
			ent.SetAnchor(0.5f, 0.5f);
			game.Haters.Add(new Hater { Ent = ent, Pos = pos, Type = type });
		}

		private void SpawnCollectable(DudeGame game, PowerupDef def)
		{
			Vec2 pos = new Vec2((game.Rng.NextSingle() * 24) - 12, (game.Rng.NextSingle() * 14) - 7);
			var ent = SceneFactory.CreateQuad(pos.X, pos.Y, 0.6f, 0.6f, def.R, def.G, def.B, layer: 8);
			ent.SetAnchor(0.5f, 0.5f);
			game.Collectables.Add(new Collectable { Ent = ent, Pos = pos, Definition = def });
		}

		private void SpawnGem(DudeGame game, Vec2 pos, int value)
		{
			float r = value > 10 ? 0.2f : 0.4f;
			float g = value > 10 ? 0.2f : 1.0f;
			float b = value > 10 ? 1.0f : 0.8f;
			float size = value > 10 ? 0.45f : 0.3f;
			var ent = SceneFactory.CreateQuad(pos.X, pos.Y, size, size, r, g, b, layer: 9);
			ent.SetAnchor(0.5f, 0.5f);
			game.Gems.Add(new XPGem { Ent = ent, Pos = pos, Value = value });
		}

		// --- EFFECTS ---

		private void UpdateTrails(DudeGame game, float dt)
		{
			for (int i = game.Trails.Count - 1; i >= 0; i--)
			{
				var t = game.Trails[i];
				t.Alpha -= dt * 3.0f;
				if (t.Alpha <= 0) { t.Ent.Destroy(); game.Trails.RemoveAt(i); }
				else
				{
					t.Ent.SetColor(0.2f * t.Alpha, 1.0f * t.Alpha, 0.2f * t.Alpha);
					t.Ent.SetSize(t.InitW * t.Alpha, t.InitH * t.Alpha);
				}
			}
		}

		private void UpdateParticles(DudeGame game, float dt)
		{
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

		private void UpdateShake(DudeGame game, float dt)
		{
			if (game.ShakeAmount > 0) { game.ShakeAmount -= dt * 2.0f; if (game.ShakeAmount < 0) game.ShakeAmount = 0; }
		}

		private void UpdateDiscoLights(DudeGame game, float dt)
		{
			game.DiscoTimer += dt;
			if (game.ChillTimer > 0) return;
			float r = 0.1f + 0.05f * MathF.Sin(game.DiscoTimer * 2f);
			float g = 0.1f + 0.05f * MathF.Sin(game.DiscoTimer * 1.5f);
			float b = 0.15f + 0.05f * MathF.Sin(game.DiscoTimer * 0.8f);
			game.Bg.SetColor(r, g, b);
		}
	}
}