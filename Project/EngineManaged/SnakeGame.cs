using System;
using System.Collections.Generic;

public static class SnakeGame
{
	// Viewport (visible grid)
	private const int VIEW_W = 80;
	private const int VIEW_H = 50;

	// World (sim grid)
	private const int WORLD_W = 120;
	private const int WORLD_H = 120;

	// Visual sizing
	private static float _cellSize = 0.34f;
	private static float _cellSpacing = 0.30f;

	// Tick
	private static float _tick = 0.06f; // slightly slower = nicer
	private static float _accum = 0f;
	private static float _time = 0f;

	// Visual handles
	private static ulong[,] _view = new ulong[VIEW_W, VIEW_H];

	// World data
	private enum Terrain : byte { Grass, Rock, Water }
	private struct Tile
	{
		public Terrain Terrain;
		public bool Blocked;
		public bool Food;
	}
	private static Tile[,] _world = new Tile[WORLD_W, WORLD_H];

	// Snake
	private static readonly List<Int2> _snake = new();
	private static Int2 _dir = new(1, 0);
	private static int _grow = 0;

	private static Int2 _food;
	private static readonly Random _rng = new(1337);

	// Colors (tweakable palette)
	private static readonly Vec3 GRASS_A = new(0.10f, 0.16f, 0.10f);
	private static readonly Vec3 GRASS_B = new(0.08f, 0.14f, 0.09f);
	private static readonly Vec3 ROCK = new(0.18f, 0.18f, 0.20f);
	private static readonly Vec3 WATER = new(0.07f, 0.12f, 0.22f);

	private static readonly Vec3 SNAKE_BODY = new(0.12f, 0.75f, 0.18f);
	private static readonly Vec3 SNAKE_HEAD = new(0.30f, 0.92f, 0.40f);

	private static readonly Vec3 FOOD_BASE = new(0.85f, 0.18f, 0.25f);

	public static void Init()
	{
		GenerateWorldSimple();
		ResetSnake();
		SpawnFood();

		BuildViewportVisuals();
		PaintViewport();
		Native.Engine_Log("SnakeGame.Init OK");
	}

	public static void Update(float dt)
	{
		_time += dt;
		HandleInput();

		_accum += dt;
		while (_accum >= _tick)
		{
			_accum -= _tick;
			Step();
		}

		PaintViewport();
	}

	// ------------------------------------------------------------
	// World / Snake
	// ------------------------------------------------------------

	private static void GenerateWorldSimple()
	{
		// Mostly grass + a few obstacle clusters.
		for (int y = 0; y < WORLD_H; y++)
			for (int x = 0; x < WORLD_W; x++)
				_world[x, y] = new Tile { Terrain = Terrain.Grass, Blocked = false, Food = false };

		// Rocks clusters
		for (int i = 0; i < 60; i++)
		{
			int cx = _rng.Next(0, WORLD_W);
			int cy = _rng.Next(0, WORLD_H);
			int r = _rng.Next(2, 6);

			for (int y = cy - r; y <= cy + r; y++)
				for (int x = cx - r; x <= cx + r; x++)
				{
					if (!InWorld(x, y)) continue;
					if ((_rng.NextDouble() < 0.65) && (DistSq(x, y, cx, cy) <= r * r))
					{
						_world[x, y].Terrain = Terrain.Rock;
						_world[x, y].Blocked = true;
					}
				}
		}

		// A few water blobs
		for (int i = 0; i < 25; i++)
		{
			int cx = _rng.Next(0, WORLD_W);
			int cy = _rng.Next(0, WORLD_H);
			int r = _rng.Next(3, 8);

			for (int y = cy - r; y <= cy + r; y++)
				for (int x = cx - r; x <= cx + r; x++)
				{
					if (!InWorld(x, y)) continue;
					if ((_rng.NextDouble() < 0.55) && (DistSq(x, y, cx, cy) <= r * r))
					{
						_world[x, y].Terrain = Terrain.Water;
						_world[x, y].Blocked = true;
					}
				}
		}
	}

	private static void ResetSnake()
	{
		_snake.Clear();
		_snake.Add(new Int2(WORLD_W / 2, WORLD_H / 2));
		_dir = new Int2(1, 0);
		_grow = 6;
	}

	private static void SpawnFood()
	{
		// Clear old
		if (InWorld(_food.X, _food.Y))
			_world[_food.X, _food.Y].Food = false;

		for (int tries = 0; tries < 10000; tries++)
		{
			int x = _rng.Next(0, WORLD_W);
			int y = _rng.Next(0, WORLD_H);
			if (_world[x, y].Blocked) continue;
			if (IsSnakeAt(x, y)) continue;

			_food = new Int2(x, y);
			_world[x, y].Food = true;
			return;
		}

		// If we somehow fail (very full board), just reset
		ResetSnake();
	}

	private static void Step()
	{
		Int2 head = _snake[0];
		Int2 next = new Int2(head.X + _dir.X, head.Y + _dir.Y);

		// Wrap edges (feels nicer than hard clamp)
		next = new Int2(Wrap(next.X, WORLD_W), Wrap(next.Y, WORLD_H));

		if (_world[next.X, next.Y].Blocked || IsSnakeAt(next.X, next.Y))
		{
			ResetSnake();
			SpawnFood();
			return;
		}

		_snake.Insert(0, next);

		if (_world[next.X, next.Y].Food)
		{
			_world[next.X, next.Y].Food = false;
			_grow += 4;
			SpawnFood();
		}

		if (_grow > 0) _grow--;
		else _snake.RemoveAt(_snake.Count - 1);
	}

	// ------------------------------------------------------------
	// Input
	// ------------------------------------------------------------

	private static void HandleInput()
	{
		// Your current assumption: W/A/S/D with ASCII codes
		// If you switch to VK_ codes, adjust here.
		const int W = 87, A = 65, S = 83, D = 68;

		Int2 newDir = _dir;

		if (Native.Input_GetKeyDown(W)) newDir = new Int2(0, 1);
		else if (Native.Input_GetKeyDown(S)) newDir = new Int2(0, -1);
		else if (Native.Input_GetKeyDown(A)) newDir = new Int2(-1, 0);
		else if (Native.Input_GetKeyDown(D)) newDir = new Int2(1, 0);

		if (!IsReverse(newDir, _dir))
			_dir = newDir;
	}

	// ------------------------------------------------------------
	// Viewport visuals
	// ------------------------------------------------------------

	private static void BuildViewportVisuals()
	{
		for (int vx = 0; vx < VIEW_W; vx++)
			for (int vy = 0; vy < VIEW_H; vy++)
			{
				float px = (vx - VIEW_W * 0.5f) * _cellSpacing;
				float py = (vy - VIEW_H * 0.5f) * _cellSpacing;

				// Start as grass A
				_view[vx, vy] = Native.Entity_CreateQuad(px, py, _cellSize, _cellSize, GRASS_A.X, GRASS_A.Y, GRASS_A.Z);
			}
	}

	private static void PaintViewport()
	{
		Int2 head = _snake[0];

		int originX = head.X - VIEW_W / 2;
		int originY = head.Y - VIEW_H / 2;

		// Food pulse
		float pulse = 0.65f + 0.35f * (float)Math.Sin(_time * 6.0f);

		for (int vx = 0; vx < VIEW_W; vx++)
			for (int vy = 0; vy < VIEW_H; vy++)
			{
				int wx = Wrap(originX + vx, WORLD_W);
				int wy = Wrap(originY + vy, WORLD_H);

				Vec3 baseCol = BaseTileColor(wx, wy, vx, vy);

				// Overlays: food, snake
				if (_world[wx, wy].Food)
				{
					// brighten + pulse
					Vec3 c = FOOD_BASE * (0.85f + 0.25f * pulse);
					SetCell(vx, vy, c);
					continue;
				}

				int snakeIndex = SnakeIndexAt(wx, wy);
				if (snakeIndex >= 0)
				{
					// Head brighter, body slightly darkens with distance
					float t = Math.Clamp(snakeIndex / 24.0f, 0.0f, 1.0f);
					Vec3 c = Lerp(SNAKE_HEAD, SNAKE_BODY, t);

					// optional tiny "eye" effect: if head and moving toward this cell offset, brighten a bit
					if (snakeIndex == 0)
						c = c * 1.05f;

					SetCell(vx, vy, c);
					continue;
				}

				SetCell(vx, vy, baseCol);
			}
	}

	private static Vec3 BaseTileColor(int wx, int wy, int vx, int vy)
	{
		Terrain t = _world[wx, wy].Terrain;

		// Checkerboard grass to make the grid read better
		bool checker = ((wx + wy) & 1) == 0;

		return t switch
		{
			Terrain.Grass => checker ? GRASS_A : GRASS_B,
			Terrain.Rock => ROCK,
			Terrain.Water => WATER,
			_ => GRASS_A
		};
	}

	private static void SetCell(int vx, int vy, Vec3 c)
	{
		ulong id = _view[vx, vy];
		if (id == 0) return;
		Native.Visual_SetColor(id, c.X, c.Y, c.Z);
	}

	// ------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------

	private static bool InWorld(int x, int y) => (uint)x < WORLD_W && (uint)y < WORLD_H;
	private static int Wrap(int v, int mod) => (v % mod + mod) % mod;
	private static int DistSq(int x, int y, int cx, int cy) { int dx = x - cx, dy = y - cy; return dx * dx + dy * dy; }

	private static bool IsSnakeAt(int x, int y)
	{
		for (int i = 0; i < _snake.Count; i++)
			if (_snake[i].X == x && _snake[i].Y == y) return true;
		return false;
	}

	private static int SnakeIndexAt(int x, int y)
	{
		for (int i = 0; i < _snake.Count; i++)
			if (_snake[i].X == x && _snake[i].Y == y) return i;
		return -1;
	}

	private static bool IsReverse(Int2 a, Int2 b) => (a.X == -b.X && a.Y == -b.Y);

	private static Vec3 Lerp(Vec3 a, Vec3 b, float t) => new(
		a.X + (b.X - a.X) * t,
		a.Y + (b.Y - a.Y) * t,
		a.Z + (b.Z - a.Z) * t
	);

	private readonly struct Int2
	{
		public readonly int X, Y;
		public Int2(int x, int y) { X = x; Y = y; }
	}

	private readonly struct Vec3
	{
		public readonly float X, Y, Z;
		public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }

		public static Vec3 operator *(Vec3 a, float s) => new(a.X * s, a.Y * s, a.Z * s);
	}
}
