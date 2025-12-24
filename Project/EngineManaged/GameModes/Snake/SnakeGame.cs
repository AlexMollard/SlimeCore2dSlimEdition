using EngineManaged.Numeric;
using EngineManaged.Scene;
using EngineManaged.UI;
using EngineManaged.Rendering;
using SlimeCore.Core.Grid;
using SlimeCore.Core.World;
using SlimeCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SlimeCore.GameModes.Snake
{
	public class SnakeGame : IGameMode
	{
        private const int VIEW_W = 100;
		private const int VIEW_H = 75;

		private const int WORLD_W = 240;
		private const int WORLD_H = 240;
        private GridSystem<Terrain> _world { get; set; }

		private static float _cellSize = 0.4f;
		private static float _cellSpacing = 0.4f;

		// Logic constants
		private const float TICK_NORMAL = 0.12f;
		private const float TICK_SPRINT = 0.05f;

		// Game Logic
		private static float _tick = TICK_NORMAL;
		private static float _accum = 0f;
		private static float _time = 0f;
		private static float _shake = 0f;

		private static float _speedBoostTimer = 0f;

        // Visual Handles
        private Entity[,] _view = new Entity[VIEW_W, VIEW_H];

        // Food
        private static int _foodCount = 0;
		private const int MAX_FOOD = 25;
		private const int START_FORWARD_CLEAR = 3;

		private enum FoodType { None, Apple, Gold, Plum, Chili }
		private static FoodType[,] _foodMap = new FoodType[WORLD_W, WORLD_H];

		// Camera & Smoothing
		private Vec2 _cam;
        private PlayerSnake _snake { get; set; } = new();
		private ParticleSystem _particleSys;

		// UI
		private static UIText _score;
		private static UIButton _testBtn;
		private static int _scoreCached = -1;
		private const int SCORE_FONT_SIZE = 1;
		private static int _currentScore = 0;

		
		private static Random _rng = new Random();
		private static int _seed = 0;

		// --- Palette ---
		private static readonly Vec3 COL_GRASS_1 = new(0.05f, 0.05f, 0.12f);
		private static readonly Vec3 COL_GRASS_2 = new(0.03f, 0.03f, 0.10f);
		private static readonly Vec3 COL_ROCK = new(0.20f, 0.20f, 0.35f);
		private static readonly Vec3 COL_WATER = new(0.10f, 0.60f, 0.80f);
		private static readonly Vec3 COL_LAVA = new(1.00f, 0.20f, 0.00f);
		private static readonly Vec3 COL_MUD_1 = new(0.12f, 0.06f, 0.06f);
		private static readonly Vec3 COL_MUD_2 = new(0.08f, 0.04f, 0.04f);
		private static readonly Vec3 COL_ICE_1 = new(0.60f, 0.90f, 1.00f);
		private static readonly Vec3 COL_ICE_2 = new(0.45f, 0.75f, 0.95f);
		private static readonly Vec3 COL_SPEED_1 = new(0.80f, 0.80f, 0.40f);
		private static readonly Vec3 COL_SPEED_2 = new(0.60f, 0.60f, 0.30f);
		private static readonly Vec3 COL_TINT_ICE = new(0.80f, 1.00f, 1.00f);
		private static readonly Vec3 COL_TINT_MUD = new(0.30f, 0.20f, 0.15f);
		private static readonly Vec3 COL_FOOD_APPLE = new(1.00f, 0.00f, 0.90f);
		private static readonly Vec3 COL_FOOD_GOLD = new(1.00f, 0.85f, 0.00f);
		private static readonly Vec3 COL_FOOD_PLUM = new(0.60f, 0.20f, 0.90f);
		private static readonly Vec3 COL_FOOD_CHILI = new(1.00f, 0.20f, 0.00f);

		public void Init()
		{
			_world = new GridSystem<Terrain>(WORLD_W, WORLD_H, Terrain.Grass);
			_particleSys = new ParticleSystem(5000);

			ResetGameLogic();

			for (int x = 0; x < VIEW_W; x++)
			{
				for (int y = 0; y < VIEW_H; y++)
				{
					_view[x, y] = SceneFactory.CreateQuad(0, 0, _cellSize, _cellSize, 1f, 1f, 1f, layer: 0);
					var transform = _view[x, y].GetComponent<TransformComponent>();
					transform.Anchor = (0.5f, 0.5f);
				}
			}
			IntPtr textureId = Native.Resources_LoadTexture("Debug", "textures/debug.png");
			_snake.Initialize(_cellSize);

			_score = UIText.Create("SCORE: 0", SCORE_FONT_SIZE, -15.0f, 7.5f);
			_score.IsVisible = true;
			_scoreCached = -1;

			_testBtn = UIButton.Create("Click me", x: 0.0f, y: 6.0f, w: 3.75f, h: 1.5f, r: 0.2f, g: 0.6f, b: 0.9f, layer: 100, fontSize: 1);
			_testBtn.Clicked += () => { Logger.Info("PRESSED!"); };
		}

		public void Shutdown()
		{
			UISystem.Clear();
			_score.Destroy();
			_testBtn.Destroy();
			_snake.Destroy();
			_particleSys.Dispose();

			for (int x = 0; x < VIEW_W; x++)
				for (int y = 0; y < VIEW_H; y++)
					_view[x, y].Destroy();
		}

		private void ResetGameLogic(int? seed = null)
		{
			if (seed.HasValue) _seed = seed.Value;
			else _seed = new Random().Next();

			_rng = new Random(_seed);
			Logger.Info($"Seed: {_seed}");

			var gen = new SlimeCore.Core.World.WorldGenerator(_seed);
			gen.Generate(_world);

			_foodMap = new FoodType[WORLD_W, WORLD_H];
			_foodCount = 0;
			_currentScore = 0;

			_accum = 0f;
			_time = 0f;
			_tick = TICK_NORMAL;
			_snake.IsSprinting = false;
			_speedBoostTimer = 0f;

			var center = new Vec2i(WORLD_W / 2, WORLD_H / 2);
			var (start, dir) = FindSafeStart(center, 60, START_FORWARD_CLEAR);

			_snake.Clear();
			_snake.Add(start);
			_snake.Add(new Vec2i(Wrap(start.X - dir.X, WORLD_W), Wrap(start.Y - dir.Y, WORLD_H)));
			_snake.Direction = dir;
			_snake.NextDirection = _snake.Direction;
			_snake.Grow = 4;
			_snake.IsDead = false;
			_cam = start.ToVec2();
			SpawnFood();
		}

		public void Update(float dt)
		{
			_particleSys.OnUpdate(dt);
			_time += dt;
			_shake = Math.Max(0, _shake - dt * 2.0f);

			if (_speedBoostTimer > 0)
			{
				_speedBoostTimer -= dt;
				if (_speedBoostTimer < 0) _speedBoostTimer = 0;
			}

			if (!_snake.IsDead)
			{
				HandleInput();

				var headTile = _world[_snake[0].X, _snake[0].Y].Type;
				float speedMultiplier = 1.0f;

				if (headTile == Terrain.Mud) speedMultiplier = 1.8f;
				if (headTile == Terrain.Speed) speedMultiplier = 0.5f;

				bool effectivelySprinting = _snake.IsSprinting || _speedBoostTimer > 0;
				float baseTick = effectivelySprinting ? TICK_SPRINT : TICK_NORMAL;

				_tick = baseTick * speedMultiplier;

				_accum += dt;
				while (_accum >= _tick)
				{
					_accum -= _tick;
					_snake.Direction = _snake.NextDirection;
					Step();
				}
			}
			else
			{
				if (Input.GetKeyDown(Keycode.SPACE)) ResetGameLogic();

				if (_snake.Body.Count > 1)
				{
					_accum += dt;
					while (_accum >= _tick)
					{
						_accum -= _tick;
						_snake.RemoveAt(_snake.Body.Count - 1);
					}
				}
			}

			// Smooth Camera
			float interp = _accum / _tick;

			// FIX: Cast Vec2i to Vec2 before scalar multiplication
			Vec2 target = _snake[0] + ((Vec2)_snake.Direction * interp);

			// Calculate wrapped distance vector
			Vec2 d = target - _cam;

			// Handle toroidal wrapping for smoothing
			if (d.X > WORLD_W * 0.5f) d.X -= WORLD_W;
			else if (d.X < -WORLD_W * 0.5f) d.X += WORLD_W;

			if (d.Y > WORLD_H * 0.5f) d.Y -= WORLD_H;
			else if (d.Y < -WORLD_H * 0.5f) d.Y += WORLD_H;

			float smoothSpeed = Math.Clamp(dt * 10.0f, 0f, 1f);

			_cam += d * smoothSpeed;

			// Wrap camera position cleanly
			_cam.X = (_cam.X % WORLD_W + WORLD_W) % WORLD_W;
			_cam.Y = (_cam.Y % WORLD_H + WORLD_H) % WORLD_H;

			Render(interp);
		}

		private (Vec2i head, Vec2i dir) FindSafeStart(Vec2i center, int maxRadius = 60, int requiredForwardClear = 3)
		{
			Vec2i[] dirs = new[] { new Vec2i(1, 0), new Vec2i(0, 1), new Vec2i(-1, 0), new Vec2i(0, -1) };
			for (int r = 0; r <= maxRadius; r++)
			{
				for (int dx = -r; dx <= r; dx++)
				{
					for (int dy = -r; dy <= r; dy++)
					{
						if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;

						int x = Wrap(center.X + dx, WORLD_W);
						int y = Wrap(center.Y + dy, WORLD_H);

						if (_world[x, y].Blocked) continue;

						foreach (var d in dirs)
						{
							int tx = Wrap(x - d.X, WORLD_W);
							int ty = Wrap(y - d.Y, WORLD_H);
							if (_world[tx, ty].Blocked) continue;

							bool ok = true;
							for (int i = 1; i <= requiredForwardClear; i++)
							{
								Vec2i checkPos = new Vec2i(x, y) + (d * i);
								if (_world[Wrap(checkPos.X, WORLD_W), Wrap(checkPos.Y, WORLD_H)].Blocked)
								{
									ok = false; break;
								}
							}
							if (ok) return (new Vec2i(x, y), d);
						}
					}
				}
			}
			return (center, new Vec2i(1, 0));
		}

		private void Step()
		{
			Vec2i head = _snake[0];
			Vec2i nextRaw = head + _snake.Direction;
			Vec2i next = new Vec2i(Wrap(nextRaw.X, WORLD_W), Wrap(nextRaw.Y, WORLD_H));
			
			if (_world[next].Blocked && _snake.IsSprinting)
			{
                //Drill Baby Drill
                if (_snake.IsSprinting)
                {
                    _world.Set(next, e =>
                    {
                        e.Blocked = false;
                        e.Type = Terrain.Grass;
                        e.Food = false;
                    });
                    SpawnExplosion(next, 50, COL_ROCK);
                }
            }
            //Next tile blocked or snake collision = death
            if (_world[next].Blocked || IsSnakeAt(next.X, next.Y))
			{
				_snake.IsDead = true;
				_shake = 0.4f;
				SpawnExplosion(next, 50, new Vec3(1.0f, 0.2f, 0.2f));
				return;
			}

			_snake.Insert(0, next);

			if (_world[next.X, next.Y].Food)
			{
				_world[next.X, next.Y].Food = false;
				FoodType type = _foodMap[next.X, next.Y];
				_foodMap[next.X, next.Y] = FoodType.None;
				_foodCount--;

				Vec3 pCol = COL_FOOD_APPLE;
				switch (type)
				{
					case FoodType.Gold: _snake.Grow += 5; _currentScore += 5; _shake = 0.3f; pCol = COL_FOOD_GOLD; break;
					case FoodType.Plum: _snake.Grow -= 2; _currentScore += 2; _shake = 0.15f; pCol = COL_FOOD_PLUM; break;
					case FoodType.Chili: _snake.Grow += 3; _currentScore += 1; _speedBoostTimer += 3.0f; _shake = 0.2f; pCol = COL_FOOD_CHILI; break;
					case FoodType.Apple: default: _snake.Grow += 3; _currentScore += 1; _shake = 0.15f; pCol = COL_FOOD_APPLE; break;
				}
				SpawnExplosion(next, 15, pCol);
				SpawnFood();
			}

			if (_snake.Grow > 0)
			{
				_snake.Grow--;
			}
			else if (_snake.Grow < 0)
			{
				if (_snake.Body.Count > 1)
				{
					_snake.RemoveAt(_snake.Body.Count - 1);
				}
				if (_snake.Body.Count > 1)
				{
					_snake.RemoveAt(_snake.Body.Count - 1);
				}
				_snake.Grow++;
			}
			else if (_snake.Body.Count > 0)
			{
				_snake.RemoveAt(_snake.Body.Count - 1);
			}

			if (_snake.Body.Count == 0)
			{
				_snake.Add(next);
			}
		}

		private void SpawnFood()
		{
			while (_foodCount < MAX_FOOD)
			{
				bool spawned = false;
				for (int i = 0; i < 100; i++)
				{
					int range = 60;
					Vec2i rndOffset = new Vec2i(_rng.Next(-range, range), _rng.Next(-range, range));
					Vec2i pos = _snake[0] + rndOffset;

					int x = Wrap(pos.X, WORLD_W);
					int y = Wrap(pos.Y, WORLD_H);

					if (!_world[x, y].Blocked && 
						!_world[x, y].Food && 
						_snake.GetBodyIndexFromWorldPosition(x, y) == -1)
					{
						_world[x, y].Food = true;
						_foodCount++;
						spawned = true;
						double roll = _rng.NextDouble();
						if (roll < 0.10) _foodMap[x, y] = FoodType.Gold;
						else if (roll < 0.20) _foodMap[x, y] = FoodType.Chili;
						else if (roll < 0.35) _foodMap[x, y] = FoodType.Plum;
						else _foodMap[x, y] = FoodType.Apple;
						break;
					}
				}
				if (!spawned) break;
			}
		}

		private void SpawnExplosion(Vec2i worldPos, int count, Vec3 color)
		{
			// Calculate relative position to camera
			float dx = worldPos.X - _cam.X;
			float dy = worldPos.Y - _cam.Y;

			// Handle wrapping
			if (dx > WORLD_W / 2f) dx -= WORLD_W;
			else if (dx < -WORLD_W / 2f) dx += WORLD_W;

			if (dy > WORLD_H / 2f) dy -= WORLD_H;
			else if (dy < -WORLD_H / 2f) dy += WORLD_H;

			float px = dx * _cellSpacing;
			float py = dy * _cellSpacing;

			ParticleProps props = new ParticleProps();
			props.Position = new Vec2(px, py);
			props.VelocityVariation = new Vec2(2.0f, 2.0f);
			props.ColorBegin = new Color(color.X, color.Y, color.Z, 1.0f);
			props.ColorEnd = new Color(color.X, color.Y, color.Z, 0.0f);
			props.SizeBegin = 0.3f;
			props.SizeEnd = 0.0f;
			props.SizeVariation = 0.1f;
			props.LifeTime = 0.8f;

			for (int i = 0; i < count; i++)
			{
				float angle = (float)_rng.NextDouble() * 6.28f;
				float speed = (float)_rng.NextDouble() * 2.0f + 0.5f;
				props.Velocity = new Vec2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;
				
				_particleSys.Emit(props);
			}
		}

		private void Render(float interp)
		{
			Vec2 camFloor = new Vec2((float)Math.Floor(_cam.X), (float)Math.Floor(_cam.Y));
			Vec2 camFrac = _cam - camFloor;

			Vec2 shakeVec = new Vec2(((float)_rng.NextDouble() - 0.5f) * _shake, ((float)_rng.NextDouble() - 0.5f) * _shake);

			Vec2 headScr = (camFrac * -1.0f + shakeVec) * _cellSpacing;

			Vec2i headPos = _snake[0];
			Terrain headTerrain = _world[headPos.X, headPos.Y].Type;

			Vec3 snakeBase = (_snake.IsSprinting || _speedBoostTimer > 0) ? PlayerSnake.COL_SNAKE_SPRINT : PlayerSnake.COL_SNAKE;
			Vec3 activeSnakeColor = snakeBase;

			if (headTerrain == Terrain.Ice)
			{
				activeSnakeColor = Vec3.Lerp(snakeBase, COL_TINT_ICE, 0.6f);
			}
			else if (headTerrain == Terrain.Mud)
			{
				activeSnakeColor = Vec3.Lerp(snakeBase, COL_TINT_MUD, 0.5f) * 0.7f;
			}

			for (int vx = 0; vx < VIEW_W; vx++)
			{
				for (int vy = 0; vy < VIEW_H; vy++)
				{
					int wx = Wrap((int)camFloor.X - VIEW_W / 2 + vx, WORLD_W);
					int wy = Wrap((int)camFloor.Y - VIEW_H / 2 + vy, WORLD_H);

					float px = (vx - VIEW_W / 2f - camFrac.X + shakeVec.X) * (_cellSpacing);
					float py = (vy - VIEW_H / 2f - camFrac.Y + shakeVec.Y) * (_cellSpacing);

                    var ent = _view[vx, vy];
					var transform = ent.GetComponent<TransformComponent>();
					transform.Position = (px, py);

					Vec3 tileCol = GetTileColor(wx, wy);

					if (_world[wx, wy].Food)
					{
						FoodType f = _foodMap[wx, wy];
						Vec3 fCol = COL_FOOD_APPLE;
						if (f == FoodType.Gold) fCol = COL_FOOD_GOLD;
						else if (f == FoodType.Plum) fCol = COL_FOOD_PLUM;
						else if (f == FoodType.Chili) fCol = COL_FOOD_CHILI;

						tileCol = fCol * (0.8f + 0.2f * (float)Math.Sin(_time * 12f));
					}

					int sIdx = _snake.GetBodyIndexFromWorldPosition(wx, wy);
					if (sIdx != -1)
					{
						float tailFade = (1.0f - (sIdx * 0.02f));
						Vec3 finalSnakeCol = activeSnakeColor * tailFade;
						if (_snake.IsDead) finalSnakeCol = new Vec3(0.4f, 0.1f, 0.1f);

						if (sIdx == 0)
						{
							var headTransform = _snake.Head.GetComponent<TransformComponent>();
							headTransform.Scale = (_cellSize * 1.15f, _cellSize * 1.15f);
							headTransform.Position = (px, py);

							var headSprite = _snake.Head.GetComponent<SpriteComponent>();
							headSprite.Color = (finalSnakeCol.X, finalSnakeCol.Y, finalSnakeCol.Z);
							UpdateEyes(px, py, _cellSize * 1.15f);
						}
						var sprite = ent.GetComponent<SpriteComponent>();
						sprite.Color = (finalSnakeCol.X, finalSnakeCol.Y, finalSnakeCol.Z);
					}
					else
					{
						var sprite = ent.GetComponent<SpriteComponent>();
						sprite.Color = (tileCol.X, tileCol.Y, tileCol.Z);
					}
					var finalTransform = ent.GetComponent<TransformComponent>();
					finalTransform.Scale = (_cellSize, _cellSize);
				}
			}

			UpdateFoodCompass(headScr);

			if (_currentScore != _scoreCached)
			{
				_scoreCached = _currentScore;
				_score.Text = $"SCORE: {_currentScore}";
			}
		}

		private void UpdateFoodCompass(Vec2 headScreenPos)
		{
			Vec2i nearest = GetNearestFoodPos();
			if (nearest.X != -1)
			{
				Vec2i head = _snake[0];

				float dx = nearest.X - head.X;
				if (Math.Abs(dx) > WORLD_W / 2) dx = -Math.Sign(dx) * (WORLD_W - Math.Abs(dx));

				float dy = nearest.Y - head.Y;
				if (Math.Abs(dy) > WORLD_H / 2) dy = -Math.Sign(dy) * (WORLD_H - Math.Abs(dy));

				Vec2 dirVec = new Vec2(dx, dy);
				Vec2 normalizedDir = dirVec.Normalized();

				float orbitDist = _cellSpacing * 0.9f;

				var compassSprite = _snake.Compass.GetComponent<SpriteComponent>();
				compassSprite.IsVisible = true;
				Vec2 compassPos = headScreenPos + normalizedDir * orbitDist;

				var compassTransform = _snake.Compass.GetComponent<TransformComponent>();
				compassTransform.Position = (compassPos.X, compassPos.Y);

				float pulse = 0.5f + 0.5f * (float)Math.Abs(Math.Sin(_time * 10f));

				Vec3 cCol = new Vec3(pulse, pulse, 0);
				FoodType ft = _foodMap[nearest.X, nearest.Y];
				if (ft == FoodType.Plum) cCol = new Vec3(pulse, 0, pulse);
				if (ft == FoodType.Chili) cCol = new Vec3(pulse, 0, 0);
			}
			else
			{
				var compassSprite = _snake.Compass.GetComponent<SpriteComponent>();
				compassSprite.IsVisible = false;
			}
		}

		private void UpdateEyes(float hpx, float hpy, float headSize)
		{
			float forward = headSize * 0.22f;
			float side = headSize * 0.18f;

			Vec2 basePos = new Vec2(hpx, hpy);
			// FIX: Cast _dir (Vec2i) to Vec2 before scalar multiply
			Vec2 fwdVec = (Vec2)_snake.Direction * forward;
			Vec2 sideVec = new Vec2(-_snake.Direction.Y, _snake.Direction.X) * side;

			float eyeSize = headSize * 0.17f;
			var eye0Transform = _snake.Eyes[0].GetComponent<TransformComponent>();
			eye0Transform.Scale = (eyeSize, eyeSize);
			var eye1Transform = _snake.Eyes[1].GetComponent<TransformComponent>();
			eye1Transform.Scale = (eyeSize, eyeSize);

			Vec2 p0 = basePos + fwdVec + sideVec;
			Vec2 p1 = basePos + fwdVec - sideVec;

			eye0Transform.Position = (p0.X, p0.Y);
			eye1Transform.Position = (p1.X, p1.Y);
		}

		// --- Helpers ---
		private Vec3 GetTileColor(int x, int y)
		{
			var tile = _world[x, y];
			var t = tile.Type;
			bool isAlt = (x + y) % 2 == 0;
			return t switch
			{
				Terrain.Rock => COL_ROCK,
				Terrain.Water => COL_WATER,
				Terrain.Lava => COL_LAVA,
				Terrain.Ice => isAlt ? COL_ICE_1 : COL_ICE_2,
				Terrain.Mud => isAlt ? COL_MUD_1 : COL_MUD_2,
				Terrain.Speed => (isAlt ? COL_SPEED_1 : COL_SPEED_2) * (0.8f + 0.2f * (float)Math.Sin(_time * 15f)),
				_ => isAlt ? COL_GRASS_1 : COL_GRASS_2
			};
		}

		private void HandleInput()
		{
			var headPos = _snake[0];
			if (_world[headPos.X, headPos.Y].Type == Terrain.Ice) return;
			_snake.IsSprinting = Input.GetKeyDown(Keycode.LEFT_SHIFT);
			if ((Input.GetKeyDown(Keycode.W) || Input.GetKeyDown(Keycode.UP)) && _snake.Direction.Y == 0) _snake.NextDirection = new Vec2i(0, 1);
			if ((Input.GetKeyDown(Keycode.S) || Input.GetKeyDown(Keycode.DOWN)) && _snake.Direction.Y == 0) _snake.NextDirection = new Vec2i(0, -1);
			if ((Input.GetKeyDown(Keycode.A) || Input.GetKeyDown(Keycode.LEFT)) && _snake.Direction.X == 0) _snake.NextDirection = new Vec2i(-1, 0);
			if ((Input.GetKeyDown(Keycode.D) || Input.GetKeyDown(Keycode.RIGHT)) && _snake.Direction.X == 0) _snake.NextDirection = new Vec2i(1, 0);
		}

		private Vec2i GetNearestFoodPos()
		{
			Vec2i head = _snake[0];
			Vec2i bestFood = new Vec2i(-1, -1);
			float minWeightedDist = float.MaxValue;
			for (int x = 0; x < WORLD_W; x++)
			{
				for (int y = 0; y < WORLD_H; y++)
				{
					if (_world[x, y].Food)
					{
						float dx = x - head.X;
						if (Math.Abs(dx) > WORLD_W / 2) dx = Math.Sign(dx) * (WORLD_W - Math.Abs(dx));

						float dy = y - head.Y;
						if (Math.Abs(dy) > WORLD_H / 2) dy = Math.Sign(dy) * (WORLD_H - Math.Abs(dy));

						Vec2 distVec = new Vec2(dx, dy);
						float dist = distVec.Length();

						float priorityBonus = 0f;
						switch (_foodMap[x, y])
						{
							case FoodType.Gold: priorityBonus = 25.0f; break;
							case FoodType.Chili: priorityBonus = 15.0f; break;
							case FoodType.Plum: priorityBonus = -5.0f; break;
						}
						float weightedDist = dist - priorityBonus;
						if (weightedDist < minWeightedDist) { minWeightedDist = weightedDist; bestFood = new Vec2i(x, y); }
					}
				}
			}
			return bestFood;
		}

		private bool IsSnakeAt(int x, int y) => _snake.GetBodyIndexFromWorldPosition(x, y) != -1;
		private int Wrap(int v, int m) => (v % m + m) % m;
	}
}