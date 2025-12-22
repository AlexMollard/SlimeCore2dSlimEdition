using SlimeCore.Core.Grid;
using SlimeCore.Core.World;
using SlimeCore.Interfaces;
using EngineManaged.Scene;
using EngineManaged.UI;
using EngineManaged.Numeric;
using System;
using System.Collections.Generic;

namespace GameModes.Snake
{
	public class SnakeGame : IGameMode
	{
		private const int VIEW_W = 100;
		private const int VIEW_H = 75;
		private const int WORLD_W = 240;
		private const int WORLD_H = 240;

		private static float _cellSize = 0.4f;
		private static float _cellSpacing = 0.4f;

		// Logic constants
		private const float TICK_NORMAL = 0.12f;
		private const float TICK_SPRINT = 0.05f;

		// Game Logic
		private static float _tick = TICK_NORMAL;
		private static float _accum = 0f;
		private static float _time = 0f;
		private static bool _isDead = false;
		private static float _shake = 0f;
		private static bool _isSprinting = false;

		private static float _speedBoostTimer = 0f;

		// Food
		private static int _foodCount = 0;
		private const int MAX_FOOD = 25;
		private const int START_FORWARD_CLEAR = 3;

		private enum FoodType { None, Apple, Gold, Plum, Chili }
		private static FoodType[,] _foodMap = new FoodType[WORLD_W, WORLD_H];

		// Camera & Smoothing
		private static Vec2 _cam;
		private static List<Vec2i> _snake = new();
		private static Vec2i _dir = new(1, 0);
		private static Vec2i _nextDir = new(1, 0);
		private static int _grow = 0;

		// Visual Handles
		private static Entity[,] _view = new Entity[VIEW_W, VIEW_H];
		private static Entity[] _eyes = new Entity[2];
		private static Entity _compass;
		private static Entity _head;

		// UI
		private static UIText _score;
		private static UIText _seedLabel;
		private static UIButton _testBtn;
		private static int _scoreCached = -1;
		private const int SCORE_FONT_SIZE = 52;
		private static int _currentScore = 0;

		private static Tile<Terrain>[,] _world = new Tile<Terrain>[WORLD_W, WORLD_H];
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
		private static readonly Vec3 COL_SNAKE = new(0.00f, 1.00f, 0.50f);
		private static readonly Vec3 COL_SNAKE_SPRINT = new(0.30f, 0.80f, 1.00f);
		private static readonly Vec3 COL_FOOD_APPLE = new(1.00f, 0.00f, 0.90f);
		private static readonly Vec3 COL_FOOD_GOLD = new(1.00f, 0.85f, 0.00f);
		private static readonly Vec3 COL_FOOD_PLUM = new(0.60f, 0.20f, 0.90f);
		private static readonly Vec3 COL_FOOD_CHILI = new(1.00f, 0.20f, 0.00f);

		public void Init()
		{
			_seedLabel = UIText.Create($"SEED: {_seed}", 28, -13.0f, 8.0f);
			_seedLabel.SetVisible(true);

			ResetGameLogic();

			for (int x = 0; x < VIEW_W; x++)
			{
				for (int y = 0; y < VIEW_H; y++)
				{
					_view[x, y] = SceneFactory.CreateQuad(0, 0, _cellSize, _cellSize, 1f, 1f, 1f, layer: 0);
					_view[x, y].SetAnchor(0.5f, 0.5f);
				}
			}

			_eyes[0] = SceneFactory.CreateQuad(0, 0, 0.08f, 0.08f, 0f, 0f, 0f, layer: 10);
			_eyes[0].SetAnchor(0.5f, 0.5f);

			_eyes[1] = SceneFactory.CreateQuad(0, 0, 0.08f, 0.08f, 0f, 0f, 0f, layer: 10);
			_eyes[1].SetAnchor(0.5f, 0.5f);

			_head = SceneFactory.CreateQuad(0, 0, _cellSize * 1.15f, _cellSize * 1.15f,
											COL_SNAKE.X, COL_SNAKE.Y, COL_SNAKE.Z, layer: 5);
			_head.SetAnchor(0.5f, 0.5f);
			_head.IsVisible = true;

			_compass = SceneFactory.CreateQuad(0, 0, 0.1f, 0.1f, 1f, 1f, 0f, layer: 20);
			_compass.SetAnchor(0.5f, 0.5f);
			_compass.IsVisible = true;

			_score = UIText.Create("SCORE: 0", SCORE_FONT_SIZE, -0.0f, 7.5f);
			_score.SetVisible(true);
			_scoreCached = -1;

			_testBtn = UIButton.Create("Click me", x: 0.0f, y: 6.0f, w: 3.75f, h: 1.5f, r: 0.2f, g: 0.6f, b: 0.9f, layer: 100, fontSize: 42);
			_testBtn.Clicked += () => { Console.WriteLine("PRESSED!"); };
		}

		public void Shutdown()
		{
			UISystem.Clear();
			_score.Destroy();
			_seedLabel.Destroy();
			_testBtn.Destroy();
			_head.Destroy();
			_compass.Destroy();
			_eyes[0].Destroy();
			_eyes[1].Destroy();

			for (int x = 0; x < VIEW_W; x++)
				for (int y = 0; y < VIEW_H; y++)
					_view[x, y].Destroy();
		}

		private void ResetGameLogic(int? seed = null)
		{
			if (seed.HasValue) _seed = seed.Value;
			else _seed = new Random().Next();

			_rng = new Random(_seed);
			Console.WriteLine($"Seed: {_seed}");
			if (_seedLabel.IsValid) _seedLabel.Text = $"SEED: {_seed}";

			var gen = new SlimeCore.Core.World.WorldGenerator(_seed);
			gen.Generate(_world);

			_foodMap = new FoodType[WORLD_W, WORLD_H];
			_foodCount = 0;
			_currentScore = 0;

			_accum = 0f;
			_time = 0f;
			_tick = TICK_NORMAL;
			_isSprinting = false;
			_speedBoostTimer = 0f;

			var center = new Vec2i(WORLD_W / 2, WORLD_H / 2);
			var (start, dir) = FindSafeStart(center, 60, START_FORWARD_CLEAR);

			_snake.Clear();
			_snake.Add(start);
			_snake.Add(new Vec2i(Wrap(start.X - dir.X, WORLD_W), Wrap(start.Y - dir.Y, WORLD_H)));
			_dir = dir;
			_nextDir = _dir;
			_grow = 4;
			_isDead = false;
			_cam = start.ToVec2();
			SpawnFood();
		}

		public void Update(float dt)
		{
			_time += dt;
			_shake = Math.Max(0, _shake - dt * 2.0f);

			if (_speedBoostTimer > 0)
			{
				_speedBoostTimer -= dt;
				if (_speedBoostTimer < 0) _speedBoostTimer = 0;
			}

			if (!_isDead)
			{
				HandleInput();

				var headTile = _world[_snake[0].X, _snake[0].Y].Type;
				float speedMultiplier = 1.0f;

				if (headTile == Terrain.Mud) speedMultiplier = 1.8f;
				if (headTile == Terrain.Speed) speedMultiplier = 0.5f;

				bool effectivelySprinting = _isSprinting || _speedBoostTimer > 0;
				float baseTick = effectivelySprinting ? TICK_SPRINT : TICK_NORMAL;

				_tick = baseTick * speedMultiplier;

				_accum += dt;
				while (_accum >= _tick)
				{
					_accum -= _tick;
					_dir = _nextDir;
					Step();
				}
			}
			else
			{
				if (Input.GetKeyDown(Keycode.SPACE)) ResetGameLogic();

				if (_snake.Count > 1)
				{
					_accum += dt;
					while (_accum >= _tick)
					{
						_accum -= _tick;
						_snake.RemoveAt(_snake.Count - 1);
					}
				}
			}

			// Smooth Camera
			float interp = _accum / _tick;

			// FIX: Cast Vec2i to Vec2 before scalar multiplication
			Vec2 target = _snake[0] + ((Vec2)_dir * interp);

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
			Vec2i nextRaw = head + _dir;
			Vec2i next = new Vec2i(Wrap(nextRaw.X, WORLD_W), Wrap(nextRaw.Y, WORLD_H));

			if (_world[next.X, next.Y].Blocked || IsSnakeAt(next.X, next.Y))
			{
				_isDead = true;
				_shake = 0.4f;
				return;
			}

			_snake.Insert(0, next);

			if (_world[next.X, next.Y].Food)
			{
				_world[next.X, next.Y].Food = false;
				FoodType type = _foodMap[next.X, next.Y];
				_foodMap[next.X, next.Y] = FoodType.None;
				_foodCount--;

				switch (type)
				{
					case FoodType.Gold: _grow += 5; _currentScore += 5; _shake = 0.3f; break;
					case FoodType.Plum: _grow -= 2; _currentScore += 2; _shake = 0.15f; break;
					case FoodType.Chili: _grow += 3; _currentScore += 1; _speedBoostTimer += 3.0f; _shake = 0.2f; break;
					case FoodType.Apple: default: _grow += 3; _currentScore += 1; _shake = 0.15f; break;
				}
				SpawnFood();
			}

			if (_grow > 0) _grow--;
			else if (_grow < 0)
			{
				if (_snake.Count > 1) _snake.RemoveAt(_snake.Count - 1);
				if (_snake.Count > 1) _snake.RemoveAt(_snake.Count - 1);
				_grow++;
			}
			else { if (_snake.Count > 0) _snake.RemoveAt(_snake.Count - 1); }

			if (_snake.Count == 0) _snake.Add(next);
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

					if (!_world[x, y].Blocked && !_world[x, y].Food && GetSnakeIndexAt(x, y) == -1)
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

		private void Render(float interp)
		{
			Vec2 camFloor = new Vec2((float)Math.Floor(_cam.X), (float)Math.Floor(_cam.Y));
			Vec2 camFrac = _cam - camFloor;

			Vec2 shakeVec = new Vec2(((float)_rng.NextDouble() - 0.5f) * _shake, ((float)_rng.NextDouble() - 0.5f) * _shake);

			Vec2 headScr = (camFrac * -1.0f + shakeVec) * _cellSpacing;

			Vec2i headPos = _snake[0];
			Terrain headTerrain = _world[headPos.X, headPos.Y].Type;

			Vec3 snakeBase = (_isSprinting || _speedBoostTimer > 0) ? COL_SNAKE_SPRINT : COL_SNAKE;
			Vec3 activeSnakeColor = snakeBase;

			if (headTerrain == Terrain.Ice) activeSnakeColor = Vec3.Lerp(snakeBase, COL_TINT_ICE, 0.6f);
			else if (headTerrain == Terrain.Mud) activeSnakeColor = Vec3.Lerp(snakeBase, COL_TINT_MUD, 0.5f) * 0.7f;

			for (int vx = 0; vx < VIEW_W; vx++)
			{
				for (int vy = 0; vy < VIEW_H; vy++)
				{
					int wx = Wrap((int)camFloor.X - VIEW_W / 2 + vx, WORLD_W);
					int wy = Wrap((int)camFloor.Y - VIEW_H / 2 + vy, WORLD_H);

					float px = (vx - VIEW_W / 2f - camFrac.X + shakeVec.X) * _cellSpacing;
					float py = (vy - VIEW_H / 2f - camFrac.Y + shakeVec.Y) * _cellSpacing;

					var ent = _view[vx, vy];
					ent.SetPosition(px, py);

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

					int sIdx = GetSnakeIndexAt(wx, wy);
					if (sIdx != -1)
					{
						float tailFade = (1.0f - (sIdx * 0.02f));
						Vec3 finalSnakeCol = activeSnakeColor * tailFade;
						if (_isDead) finalSnakeCol = new Vec3(0.4f, 0.1f, 0.1f);

						if (sIdx == 0)
						{
							_head.SetSize(_cellSize * 1.15f, _cellSize * 1.15f);
							_head.SetPosition(px, py);
							_head.SetColor(finalSnakeCol.X, finalSnakeCol.Y, finalSnakeCol.Z);
							UpdateEyes(px, py, _cellSize * 1.15f);
						}
						ent.SetColor(finalSnakeCol.X, finalSnakeCol.Y, finalSnakeCol.Z);
					}
					else
					{
						ent.SetColor(tileCol.X, tileCol.Y, tileCol.Z);
					}
					ent.SetSize(_cellSize, _cellSize);
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

				_compass.IsVisible = true;
				Vec2 compassPos = headScreenPos + normalizedDir * orbitDist;
				_compass.SetPosition(compassPos.X, compassPos.Y);

				float pulse = 0.5f + 0.5f * (float)Math.Abs(Math.Sin(_time * 10f));

				Vec3 cCol = new Vec3(pulse, pulse, 0);
				FoodType ft = _foodMap[nearest.X, nearest.Y];
				if (ft == FoodType.Plum) cCol = new Vec3(pulse, 0, pulse);
				if (ft == FoodType.Chili) cCol = new Vec3(pulse, 0, 0);
			}
			else _compass.IsVisible = false;
		}

		private void UpdateEyes(float hpx, float hpy, float headSize)
		{
			float forward = headSize * 0.22f;
			float side = headSize * 0.18f;

			Vec2 basePos = new Vec2(hpx, hpy);
			// FIX: Cast _dir (Vec2i) to Vec2 before scalar multiply
			Vec2 fwdVec = (Vec2)_dir * forward;
			Vec2 sideVec = new Vec2(-_dir.Y, _dir.X) * side;

			float eyeSize = headSize * 0.17f;
			_eyes[0].SetSize(eyeSize, eyeSize);
			_eyes[1].SetSize(eyeSize, eyeSize);

			Vec2 p0 = basePos + fwdVec + sideVec;
			Vec2 p1 = basePos + fwdVec - sideVec;

			_eyes[0].SetPosition(p0.X, p0.Y);
			_eyes[1].SetPosition(p1.X, p1.Y);
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
			_isSprinting = Input.GetKeyDown(Keycode.LEFT_SHIFT);
			if ((Input.GetKeyDown(Keycode.W) || Input.GetKeyDown(Keycode.UP)) && _dir.Y == 0) _nextDir = new Vec2i(0, 1);
			if ((Input.GetKeyDown(Keycode.S) || Input.GetKeyDown(Keycode.DOWN)) && _dir.Y == 0) _nextDir = new Vec2i(0, -1);
			if ((Input.GetKeyDown(Keycode.A) || Input.GetKeyDown(Keycode.LEFT)) && _dir.X == 0) _nextDir = new Vec2i(-1, 0);
			if ((Input.GetKeyDown(Keycode.D) || Input.GetKeyDown(Keycode.RIGHT)) && _dir.X == 0) _nextDir = new Vec2i(1, 0);
		}

		private int GetSnakeIndexAt(int x, int y)
		{
			for (int i = 0; i < _snake.Count; i++) if (_snake[i].X == x && _snake[i].Y == y) return i;
			return -1;
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

		private bool IsSnakeAt(int x, int y) => GetSnakeIndexAt(x, y) != -1;
		private int Wrap(int v, int m) => (v % m + m) % m;
	}
}