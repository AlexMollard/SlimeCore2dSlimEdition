using System;
using System.Collections.Generic;

public static class SnakeGame
{
	private const int VIEW_W = 100;
	private const int VIEW_H = 50;
	private const int WORLD_W = 240;
	private const int WORLD_H = 240;

	private static float _cellSize = 0.35f;
	private static float _cellSpacing = 0.4f;

	// Logic constants
	private const float TICK_NORMAL = 0.12f;
	private const float TICK_SPRINT = 0.05f; // Faster tick rate

	// Game Logic
	private static float _tick = TICK_NORMAL;
	private static float _accum = 0f;
	private static float _time = 0f;
	private static bool _isDead = false;
	private static float _shake = 0f;
	private static bool _isSprinting = false;

	// Food
	private static int _foodCount = 0;
	private const int MAX_FOOD = 15; // Keep 15 pieces on map at once

	// Camera & Smoothing
	private static float _camX, _camY;
	private static List<Int2> _snake = new();
	private static Int2 _dir = new(1, 0);
	private static Int2 _nextDir = new(1, 0);
	private static int _grow = 0;

	// Visual Handles
	private static ulong[,] _view = new ulong[VIEW_W, VIEW_H];
	private static ulong[] _eyes = new ulong[2];

	// Compass
	private static ulong _compassId;

	private enum Terrain : byte { Grass, Rock, Water }
	private struct Tile { public Terrain Terrain; public bool Blocked; public bool Food; }
	private static Tile[,] _world = new Tile[WORLD_W, WORLD_H];
	private static readonly Random _rng = new();

	// Palette
	private static readonly Vec3 COL_GRASS_1 = new(0.14f, 0.22f, 0.14f);
	private static readonly Vec3 COL_GRASS_2 = new(0.12f, 0.20f, 0.12f);
	private static readonly Vec3 COL_ROCK = new(0.25f, 0.25f, 0.28f);
	private static readonly Vec3 COL_WATER = new(0.10f, 0.18f, 0.35f);
	private static readonly Vec3 COL_SNAKE = new(0.40f, 0.90f, 0.30f);
	private static readonly Vec3 COL_FOOD = new(1.00f, 0.20f, 0.30f);
	private static readonly Vec3 COL_SNAKE_SPRINT = new(0.60f, 1.00f, 0.50f);

	public static void Init()
	{
		GenerateOrganicWorld();
		ResetGame();

		// Build Grid
		for (int x = 0; x < VIEW_W; x++)
			for (int y = 0; y < VIEW_H; y++)
				_view[x, y] = Native.Entity_CreateQuad(0, 0, _cellSize, _cellSize, 1, 1, 1);

		// Build Eyes (attached to head)
		_eyes[0] = Native.Entity_CreateQuad(0, 0, 0.08f, 0.08f, 0, 0, 0);
		_eyes[1] = Native.Entity_CreateQuad(0, 0, 0.08f, 0.08f, 0, 0, 0);

		_compassId = Native.Entity_CreateQuad(0, 0, 1.0f, 1.0f, 1, 1, 0); // Small yellow dot
	}

	private static Int2 GetNearestFoodPos()
	{
		Int2 head = _snake[0];
		Int2 bestFood = new Int2(-1, -1);
		float minDist = float.MaxValue;

		for (int x = 0; x < WORLD_W; x++)
		{
			for (int y = 0; y < WORLD_H; y++)
			{
				if (_world[x, y].Food)
				{
					// Calculate distance considering world wrap
					float dx = Math.Abs(x - head.X);
					if (dx > WORLD_W / 2) dx = WORLD_W - dx;

					float dy = Math.Abs(y - head.Y);
					if (dy > WORLD_H / 2) dy = WORLD_H - dy;

					float distSq = dx * dx + dy * dy;
					if (distSq < minDist)
					{
						minDist = distSq;
						bestFood = new Int2(x, y);
					}
				}
			}
		}
		return bestFood;
	}

	private static void GenerateOrganicWorld()
	{
		for (int y = 0; y < WORLD_H; y++)
		{
			for (int x = 0; x < WORLD_W; x++)
			{
				// Layer multiple sine waves at different angles to create "Noise"
				float n = 0;
				n += (float)Math.Sin(x * 0.15f + y * 0.05f);
				n += (float)Math.Sin(x * -0.1f + y * 0.22f);
				n += (float)Math.Sin(x * 0.3f + y * 0.3f) * 0.5f;

				_world[x, y] = new Tile { Terrain = Terrain.Grass };
				if (n > 1.2f) { _world[x, y].Terrain = Terrain.Rock; _world[x, y].Blocked = true; }
				else if (n < -1.4f) { _world[x, y].Terrain = Terrain.Water; _world[x, y].Blocked = true; }
			}
		}
	}

	private static void ResetGame()
	{
		_snake.Clear();
		Int2 start = new Int2(WORLD_W / 2, WORLD_H / 2);
		_snake.Add(start);
		_snake.Add(new Int2(start.X - 1, start.Y));
		_dir = new Int2(1, 0);
		_nextDir = _dir;
		_grow = 4;
		_isDead = false;
		_camX = start.X; _camY = start.Y;
		SpawnFood();
	}

	public static void Update(float dt)
	{
		_time += dt;
		_shake = Math.Max(0, _shake - dt * 2.0f);

		if (!_isDead)
		{
			HandleInput();

			// Adjust speed based on sprint state
			_tick = _isSprinting ? TICK_SPRINT : TICK_NORMAL;

			_accum += dt;
			while (_accum >= _tick)
			{
				_accum -= _tick;
				_dir = _nextDir;
				Step();
			}
		}
		else if (Native.Input_GetKeyDown(Keycode.SPACE)) ResetGame();

		// Smooth Camera following the fractional movement of the snake
		float interp = _accum / _tick;
		float targetX = _snake[0].X + (_dir.X * interp);
		float targetY = _snake[0].Y + (_dir.Y * interp);

		// Handle World Wrap for Camera
		if (Math.Abs(targetX - _camX) > 5) _camX = targetX;
		if (Math.Abs(targetY - _camY) > 5) _camY = targetY;
		_camX = Lerp(_camX, targetX, dt * 10.0f);
		_camY = Lerp(_camY, targetY, dt * 10.0f);

		Render(interp);
	}

	private static void Step()
	{
		Int2 head = _snake[0];
		Int2 next = new Int2(Wrap(head.X + _dir.X, WORLD_W), Wrap(head.Y + _dir.Y, WORLD_H));

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
			_grow += 3;
			_shake = 0.15f;
			_foodCount--;
			SpawnFood();
		}

		if (_grow > 0) _grow--;
		else _snake.RemoveAt(_snake.Count - 1);
	}

	private static void Render(float interp)
	{
		float camFracX = _camX - (float)Math.Floor(_camX);
		float camFracY = _camY - (float)Math.Floor(_camY);

		float shakeX = ((float)_rng.NextDouble() - 0.5f) * _shake;
		float shakeY = ((float)_rng.NextDouble() - 0.5f) * _shake;

		// --- NEW: Calculate the head's screen position once ---
		// Since the camera centers on the head, its screen position is 
		// always the offset from the center of the view grid.
		float headScrX = (-camFracX + shakeX) * _cellSpacing;
		float headScrY = (-camFracY + shakeY) * _cellSpacing;

		for (int vx = 0; vx < VIEW_W; vx++)
		{
			for (int vy = 0; vy < VIEW_H; vy++)
			{
				int wx = Wrap((int)Math.Floor(_camX) - VIEW_W / 2 + vx, WORLD_W);
				int wy = Wrap((int)Math.Floor(_camY) - VIEW_H / 2 + vy, WORLD_H);

				float px = (vx - VIEW_W / 2f - camFracX + shakeX) * _cellSpacing;
				float py = (vy - VIEW_H / 2f - camFracY + shakeY) * _cellSpacing;

				ulong id = _view[vx, vy];
				Native.Transform_SetPosition(id, px, py);

				Vec3 col = GetTileColor(wx, wy);
				if (_world[wx, wy].Food) col = COL_FOOD * (0.8f + 0.2f * (float)Math.Sin(_time * 12f));

				int sIdx = GetSnakeIndexAt(wx, wy);
				if (sIdx != -1)
				{
					// Use the sprint color if sprinting
					Vec3 baseCol = _isSprinting ? COL_SNAKE_SPRINT : COL_SNAKE;
					col = baseCol * (1.0f - (sIdx * 0.03f));

					if (_isDead) col = new Vec3(0.4f, 0.1f, 0.1f);

					if (sIdx == 0)
					{
						UpdateEyes(px, py); // px/py here will match headScrX/Y when at the head index
						Native.Transform_SetSize(id, _cellSize * 1.15f, _cellSize * 1.15f);
					}
					else Native.Transform_SetSize(id, _cellSize, _cellSize);
				}
				else Native.Transform_SetSize(id, _cellSize, _cellSize);

				Native.Visual_SetColor(id, col.X, col.Y, col.Z);
			}
		}

		UpdateFoodCompass(headScrX, headScrY);
	}

	private static void UpdateFoodCompass(float hpx, float hpy)
	{
		Int2 nearest = GetNearestFoodPos();
		if (nearest.X != -1)
		{
			Int2 head = _snake[0];

			// Calculate vector to food with World Wrap correction
			float dx = nearest.X - head.X;
			if (Math.Abs(dx) > WORLD_W / 2) dx = -Math.Sign(dx) * (WORLD_W - Math.Abs(dx));

			float dy = nearest.Y - head.Y;
			if (Math.Abs(dy) > WORLD_H / 2) dy = -Math.Sign(dy) * (WORLD_H - Math.Abs(dy));

			// Get angle to food
			float angle = (float)Math.Atan2(dy, dx);
			float orbitDist = 0.4f; // Distance from head center

			// Position the compass dot
			Native.Transform_SetPosition(_compassId, hpx + (float)Math.Cos(angle) * orbitDist, hpy + (float)Math.Sin(angle) * orbitDist);

			// Pulse the color so it's visible
			float pulse = 0.5f + 0.5f * (float)Math.Abs(Math.Sin(_time * 10f));
			Native.Visual_SetColor(_compassId, pulse, pulse, 0); // Yellow pulse
		}
		else
		{
			// Hide compass if no food exists (move it far away)
			Native.Transform_SetPosition(_compassId, 999, 999);
		}
	}

	private static void UpdateEyes(float hpx, float hpy)
	{
		// Offset eyes based on direction
		float ox = _dir.X * 0.1f;
		float oy = _dir.Y * 0.1f;
		// Perp vector for side-to-side spacing
		float sx = -_dir.Y * 0.08f;
		float sy = _dir.X * 0.08f;

		Native.Transform_SetPosition(_eyes[0], hpx + ox + sx, hpy + oy + sy);
		Native.Transform_SetPosition(_eyes[1], hpx + ox - sx, hpy + oy - sy);
	}

	private static Vec3 GetTileColor(int x, int y)
	{
		var t = _world[x, y].Terrain;
		if (t == Terrain.Rock) return COL_ROCK;
		if (t == Terrain.Water) return COL_WATER;
		return ((x + y) % 2 == 0) ? COL_GRASS_1 : COL_GRASS_2;
	}

	private static void HandleInput()
	{
		// I should get enums for these keycodes...
		_isSprinting = Native.Input_GetKeyDown(Keycode.LEFT_SHIFT);

		if ((Native.Input_GetKeyDown(Keycode.W) || Native.Input_GetKeyDown(Keycode.UP)) && _dir.Y == 0) _nextDir = new Int2(0, 1);
		if ((Native.Input_GetKeyDown(Keycode.S) || Native.Input_GetKeyDown(Keycode.DOWN)) && _dir.Y == 0) _nextDir = new Int2(0, -1);
		if ((Native.Input_GetKeyDown(Keycode.A) || Native.Input_GetKeyDown(Keycode.LEFT)) && _dir.X == 0) _nextDir = new Int2(-1, 0);
		if ((Native.Input_GetKeyDown(Keycode.D) || Native.Input_GetKeyDown(Keycode.RIGHT)) && _dir.X == 0) _nextDir = new Int2(1, 0);
	}

	private static void SpawnFood()
	{
		while (_foodCount < MAX_FOOD)
		{
			// Try to spawn one piece
			bool spawned = false;
			for (int i = 0; i < 100; i++) // Limit attempts to prevent infinite loops
			{
				int range = 40; // Max distance from head
				int rx = _rng.Next(-range, range);
				int ry = _rng.Next(-range, range);

				int x = Wrap(_snake[0].X + rx, WORLD_W);
				int y = Wrap(_snake[0].Y + ry, WORLD_H);

				if (!_world[x, y].Blocked && !_world[x, y].Food && GetSnakeIndexAt(x, y) == -1)
				{
					_world[x, y].Food = true;
					_foodCount++;
					spawned = true;
					break;
				}
			}
			if (!spawned) break; // Board is too full
		}
	}

	private static int GetSnakeIndexAt(int x, int y)
	{
		for (int i = 0; i < _snake.Count; i++) if (_snake[i].X == x && _snake[i].Y == y) return i;
		return -1;
	}
	private static bool IsSnakeAt(int x, int y) => GetSnakeIndexAt(x, y) != -1;
	private static int Wrap(int v, int m) => (v % m + m) % m;
	private static float Lerp(float a, float b, float t) => a + (b - a) * t;

	private struct Int2 { public int X, Y; public Int2(int x, int y) { X = x; Y = y; } }
	private struct Vec3
	{
		public float X, Y, Z; public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }
		public static Vec3 operator *(Vec3 a, float b) => new(a.X * b, a.Y * b, a.Z * b);
	}
}