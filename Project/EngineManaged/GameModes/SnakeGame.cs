using SlimeCore.Core.Grid;
using SlimeCore.Interfaces;
using System;
using System.Collections.Generic;
using static SlimeCore.Core.Numeric.Floats;
using static SlimeCore.Core.Numeric.Integrals;
using System.Security.Cryptography;

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
    private const int START_FORWARD_CLEAR = 3; // Require this many clear tiles in front of spawn

    // Camera & Smoothing
    private static float _camX, _camY;
    private static List<Int2> _snake = new();
    private static Int2 _dir = new(1, 0);
    private static Int2 _nextDir = new(1, 0);
    private static int _grow = 0;

    // Visual Handles
    private static Entity[,] _view = new Entity[VIEW_W, VIEW_H];
    private static Entity[] _eyes = new Entity[2];

    // Compass
    private static Entity _compass;

    // Dedicated head quad (separate from underlying tile)
    private static Entity _head;

    // Score UI
    private static UI.Text _score;
    private static UI.Text _seedLabel;
    private static int _scoreCached = -1;
    private const int SCORE_FONT_SIZE = 52;

    private enum Terrain : byte { Grass, Rock, Water }

    private static Tile<Terrain>[,] _world = new Tile<Terrain>[WORLD_W, WORLD_H];
    private static Random _rng = new Random();
    private static int _seed = 0;

    // Palette
    // --- Midnight Neon Palette ---
    private static readonly VecFloat3 COL_GRASS_1 = new(0.05f, 0.05f, 0.12f); // Deep Midnight
    private static readonly VecFloat3 COL_GRASS_2 = new(0.03f, 0.03f, 0.10f); // Darker Blue/Black
    private static readonly VecFloat3 COL_ROCK = new(0.20f, 0.20f, 0.35f);    // Muted Purple Rock
    private static readonly VecFloat3 COL_WATER = new(0.10f, 0.60f, 0.80f);   // Glowing Cyan Water
    private static readonly VecFloat3 COL_SNAKE = new(0.00f, 1.00f, 0.50f);   // Neon Mint
    private static readonly VecFloat3 COL_FOOD = new(1.00f, 0.00f, 0.90f);    // Hot Pink
    private static readonly VecFloat3 COL_SNAKE_SPRINT = new(0.30f, 0.80f, 1.00f); // Electric Blue

    public static void Init()
    {
        // Create seed label first so ResetGame can update it
        _seedLabel = UI.Text.Create($"SEED: {_seed}", 28, -13.0f, 8.0f);
        _seedLabel.SetVisible(true);

        ResetGame();

        // Build Grid (use overload to set color, anchor, and layer in one step)
        for (int x = 0; x < VIEW_W; x++)
            for (int y = 0; y < VIEW_H; y++)
            {
                _view[x, y] = Entity.CreateQuad(0, 0, _cellSize, _cellSize, 1f, 1f, 1f, 0.5f, 0.5f, 0);
            }

        // Build Eyes (attached to head)
        _eyes[0] = Entity.CreateQuad(0, 0, 0.08f, 0.08f, 0f, 0f, 0f, 0.5f, 0.5f, 10);
        _eyes[1] = Entity.CreateQuad(0, 0, 0.08f, 0.08f, 0f, 0f, 0f, 0.5f, 0.5f, 10);

        // Create dedicated head quad so we can scale/anchor the head independently
        _head = Entity.CreateQuad(0, 0, _cellSize * 1.15f, _cellSize * 1.15f, COL_SNAKE.X, COL_SNAKE.Y, COL_SNAKE.Z, 0.5f, 0.5f, 5);
        _head.SetVisible(true);

        _compass = Entity.CreateQuad(0, 0, 0.1f, 0.1f, 1f, 1f, 0f, 0.5f, 0.5f, 20); // Small yellow dot
        _compass.SetVisible(true); // show by default

        // Score UI (use new UI system). Position near top-left in UI coords.
        _score = UI.Text.Create("SCORE: 0", SCORE_FONT_SIZE, -30.5f, 8.5f);
        _score.SetVisible(true);
        _scoreCached = -1; // force initial text update


        var btn = UI.Button.Create("Click me", x: 0.0f, y: 6.0f, w: 3.75f, h: 1.5f, r: 0.2f, g: 0.6f, b:0.9f, layer: 100, fontSize: 42);
		btn.Clicked += () => { Console.WriteLine("PRESSED!"); };
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
        // Use seeded random to vary the noise parameters so the world changes per seed
        float ph1 = (float)(_rng.NextDouble() * Math.PI * 2.0);
        float ph2 = (float)(_rng.NextDouble() * Math.PI * 2.0);
        float ph3 = (float)(_rng.NextDouble() * Math.PI * 2.0);

        float f1 = 0.15f + (float)(_rng.NextDouble() - 0.5f) * 0.05f;
        float f2 = -0.1f + (float)(_rng.NextDouble() - 0.5f) * 0.05f;
        float f3 = 0.3f + (float)(_rng.NextDouble() - 0.5f) * 0.05f;
        float a3 = 0.5f + (float)(_rng.NextDouble() - 0.5f) * 0.3f;

        for (int y = 0; y < WORLD_H; y++)
        {
            for (int x = 0; x < WORLD_W; x++)
            {
                // Layer multiple sine waves at different angles to create "Noise"
                float n = 0;
                n += (float)Math.Sin(x * f1 + y * 0.05f + ph1);
                n += (float)Math.Sin(x * f2 + y * 0.22f + ph2);
                n += (float)Math.Sin(x * f3 + y * 0.3f + ph3) * a3;

                _world[x, y] = new Tile<Terrain>
                {
                    Type = Terrain.Grass
                };
                if (n > 1.2f)
                {
                    _world[x, y].Type = Terrain.Rock;
                    _world[x, y].Blocked = true;
                }
                else if (n < -1.4f)
                {
                    _world[x, y].Type = Terrain.Water;
                    _world[x, y].Blocked = true;
                }
            }
        }
    }

    private static void ResetGame(int? seed = null)
    {
        // Create a reproducible seed (or use provided) and reseed the RNG so each game start is repeatable
        if (seed.HasValue) _seed = seed.Value;
        else
        {
            using var rngc = RandomNumberGenerator.Create();
            var b = new byte[4];
            rngc.GetBytes(b);
            _seed = BitConverter.ToInt32(b, 0);
        }
        _rng = new Random(_seed);
        Console.WriteLine($"Seed: {_seed}");
        _seedLabel.SetText($"SEED: {_seed}");
        // Rebuild world and reset counters
        GenerateOrganicWorld();
        _foodCount = 0;

        // Reset timing and some states so the game starts fresh
        _accum = 0f;
        _time = 0f;
        _tick = TICK_NORMAL;
        _isSprinting = false;

        // Find a safe place to spawn (head + tail must be on non-blocked tiles)
        var center = new Int2(WORLD_W / 2, WORLD_H / 2);
        var (start, dir) = FindSafeStart(center, 60, START_FORWARD_CLEAR);

        _snake.Clear();
        _snake.Add(start);
        // place tail one tile behind the head
        _snake.Add(new Int2(Wrap(start.X - dir.X, WORLD_W), Wrap(start.Y - dir.Y, WORLD_H)));
        _dir = dir;
        _nextDir = _dir;
        _grow = 4;
        _isDead = false;
        _camX = start.X; _camY = start.Y;
        SpawnFood();
    }

    private static (Int2 head, Int2 dir) FindSafeStart(Int2 center, int maxRadius = 60, int requiredForwardClear = 3)
    {
        Int2[] dirs = new[] { new Int2(1, 0), new Int2(0, 1), new Int2(-1, 0), new Int2(0, -1) };

        for (int r = 0; r <= maxRadius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    // only check the perimeter of the current square to emulate a spiral
                    if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;

                    int x = Wrap(center.X + dx, WORLD_W);
                    int y = Wrap(center.Y + dy, WORLD_H);

                    if (_world[x, y].Blocked) continue;

                    foreach (var d in dirs)
                    {
                        int tx = Wrap(x - d.X, WORLD_W);
                        int ty = Wrap(y - d.Y, WORLD_H);
                        if (_world[tx, ty].Blocked) continue; // tail must fit

                        // ensure requiredForwardClear tiles ahead are not blocked
                        bool ok = true;
                        for (int i = 1; i <= requiredForwardClear; i++)
                        {
                            int fx = Wrap(x + d.X * i, WORLD_W);
                            int fy = Wrap(y + d.Y * i, WORLD_H);
                            if (_world[fx, fy].Blocked) { ok = false; break; }
                        }
                        if (!ok) continue;

                        return (new Int2(x, y), d);
                    }
                }
            }
        }

        // Fallback: scan whole map for any non-blocked head + free neighbor + forward clear
        for (int x = 0; x < WORLD_W; x++)
        {
            for (int y = 0; y < WORLD_H; y++)
            {
                if (_world[x, y].Blocked) continue;
                foreach (var d in dirs)
                {
                    int tx = Wrap(x - d.X, WORLD_W);
                    int ty = Wrap(y - d.Y, WORLD_H);
                    if (_world[tx, ty].Blocked) continue;

                    bool ok = true;
                    for (int i = 1; i <= requiredForwardClear; i++)
                    {
                        int fx = Wrap(x + d.X * i, WORLD_W);
                        int fy = Wrap(y + d.Y * i, WORLD_H);
                        if (_world[fx, fy].Blocked) { ok = false; break; }
                    }
                    if (ok) return (new Int2(x, y), d);
                }
            }
        }

        // Final fallback: force center and right-facing direction (will rarely be hit)
        return (center, new Int2(1, 0));
    }

    public static void Update(float dt)
    {
        UI.Update();

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
        else
        {
            if (Input.GetKeyDown(Keycode.SPACE))
            {
                ResetGame();
            }
            if(_snake.Count > 1)
            {
                // Slowly shrink the snake on death
                _accum += dt;
                while (_accum >= 0.2f)
                {
                    _accum -= 0.2f;
                    if (_snake.Count > 1) _snake.RemoveAt(_snake.Count - 1);
                }
            }
        }

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

        if (_grow > 0)
        {
            _grow--;
        }
        else
        {
            _snake.RemoveAt(_snake.Count - 1);
        }
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

                var ent = _view[vx, vy];
                ent.SetPosition(px, py);

                VecFloat3 col = GetTileColor(wx, wy);
                if (_world[wx, wy].Food) col = COL_FOOD * (0.8f + 0.2f * (float)Math.Sin(_time * 12f));

                int sIdx = GetSnakeIndexAt(wx, wy);
                if (sIdx != -1)
                {
                    // Use the sprint color if sprinting
                    VecFloat3 baseCol = _isSprinting ? COL_SNAKE_SPRINT : COL_SNAKE;
                    col = baseCol * (1.0f - (sIdx * 0.03f));

                    if (_isDead) col = new VecFloat3(0.4f, 0.1f, 0.1f);

                    if (sIdx == 0)
                    {
                        ent.SetSize(_cellSize, _cellSize);
                        // Update dedicated head entity so it can be sized and anchored independently
                        _head.SetSize(_cellSize * 1.15f, _cellSize * 1.15f);
                        _head.SetPosition(px, py);
                        _head.SetColor(col.X, col.Y, col.Z);
                        var (hw, hh) = _head.GetSize();
                        UpdateEyes(px, py, hw); // px/py here will match headScrX/Y when at the head index
                    }
                    else ent.SetSize(_cellSize, _cellSize);
                }
                else ent.SetSize(_cellSize, _cellSize);

                ent.SetColor(col.X, col.Y, col.Z);
            }
        }

        UpdateFoodCompass(headScrX, headScrY);

        int score = Math.Max(0, _snake.Count - 1);
        if (score != _scoreCached)
        {
            _scoreCached = score;
            _score.SetText($"SCORE: {score}");
        }

        // Position score near top-left in UI coords (account for small camera shake in world, but keep UI mostly fixed)
        float uiOffsetX = 0.0f;
        float uiOffsetY = 8.5f - 0.5f + ((float)_rng.NextDouble() - 0.5f) * _shake;
        _score.SetPosition(uiOffsetX, uiOffsetY);
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
            // Orbit distance is proportional to the cell spacing so it stays visually consistent
            float orbitDist = _cellSpacing * 0.9f;

            // Position the compass dot (and ensure it's visible)
            _compass.SetVisible(true);
            _compass.SetPosition(hpx + (float)Math.Cos(angle) * orbitDist, hpy + (float)Math.Sin(angle) * orbitDist);

            // Pulse the color so it's visible
            float pulse = 0.5f + 0.5f * (float)Math.Abs(Math.Sin(_time * 10f));
            _compass.SetColor(pulse, pulse, 0); // Yellow pulse
        }
        else
        {
            // Hide compass if no food exists
            _compass.SetVisible(false);
        }
    }

    private static void UpdateEyes(float hpx, float hpy, float headSize)
    {
        // Offsets and eye size scale with the head size to maintain alignment when head is scaled
        float forward = headSize * 0.22f; // how far forward from center the eyes sit
        float side = headSize * 0.18f; // lateral separation

        float ox = _dir.X * forward;
        float oy = _dir.Y * forward;
        float sx = -_dir.Y * side;
        float sy = _dir.X * side;

        // Make eyes a fraction of the head size (tuned to match previous visuals)
        float eyeSize = headSize * 0.17f;
        _eyes[0].SetSize(eyeSize, eyeSize);
        _eyes[1].SetSize(eyeSize, eyeSize);

        _eyes[0].SetPosition(hpx + ox + sx, hpy + oy + sy);
        _eyes[1].SetPosition(hpx + ox - sx, hpy + oy - sy);
    }

    private static VecFloat3 GetTileColor(int x, int y)
    {
        var t = _world[x, y].Type;
        if (t == Terrain.Rock) return COL_ROCK;
        if (t == Terrain.Water) return COL_WATER;
        return ((x + y) % 2 == 0) ? COL_GRASS_1 : COL_GRASS_2;
    }

    private static void HandleInput()
    {
        // I should get enums for these keycodes...
        _isSprinting = Input.GetKeyDown(Keycode.LEFT_SHIFT);

        if ((Input.GetKeyDown(Keycode.W) || Input.GetKeyDown(Keycode.UP)) && _dir.Y == 0) _nextDir = new Int2(0, 1);
        if ((Input.GetKeyDown(Keycode.S) || Input.GetKeyDown(Keycode.DOWN)) && _dir.Y == 0) _nextDir = new Int2(0, -1);
        if ((Input.GetKeyDown(Keycode.A) || Input.GetKeyDown(Keycode.LEFT)) && _dir.X == 0) _nextDir = new Int2(-1, 0);
        if ((Input.GetKeyDown(Keycode.D) || Input.GetKeyDown(Keycode.RIGHT)) && _dir.X == 0) _nextDir = new Int2(1, 0);
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

    public static void Shutdown()
    {
        throw new NotImplementedException();
    }
}