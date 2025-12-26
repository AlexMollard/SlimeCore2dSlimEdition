using EngineManaged.Numeric;
using EngineManaged.Rendering;
using EngineManaged.Scene;
using EngineManaged.UI;
using GameModes.Dude;
using SlimeCore.GameModes.Snake.Actors;
using SlimeCore.GameModes.Snake.World;
using SlimeCore.Source.Core;
using SlimeCore.Source.Input;
using SlimeCore.Source.World.Grid;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SlimeCore.GameModes.Snake;

public class SnakeGame : IGameMode
{
    private bool _isDisposed;
    public Random? Rng { get; set; }

    private const int VIEW_W = 100;
    private const int VIEW_H = 75;

    public SnakeGrid? _world { get; set; }

    public static float _cellSize = 0.4f;
    public static float _cellSpacing = 0.4f;

    // Logic constants
    private const float TICK_NORMAL = 0.12f;
    private const float TICK_SPRINT = 0.05f;

    // Game Logic
    private static float _tick = TICK_NORMAL;
    private static float _accum;
    private static float _time;
    public float _shake;

    private static float _speedBoostTimer;

    // Visual Handles
    private Entity[,] _view = new Entity[VIEW_W, VIEW_H];

    // Food
    private static int _foodCount;
    private const int MAX_FOOD = 25;
    private const int START_FORWARD_CLEAR = 3;

    private enum FoodType { None, Apple, Gold, Plum, Chili }

    private FoodType[,]? _foodMap { get; set; }

    // Camera & Smoothing
    public Vec2 _cam;
    public PlayerSnake _snake { get; set; } = new(_cellSize * 1.25f);
    private ParticleSystem? _particleSys;

    // UI
    private static UIText _score;
    private static UIButton? _testBtn;
    private static int _scoreCached = -1;
    private const int SCORE_FONT_SIZE = 1;
    public int _currentScore { get; set; }


    private static Random _rng = new();
    public float SpawnTimer;
    public float ChillTimer;
    private static int _seed;
    public SnakeGameEvents Events = new();

    //Snake Hunters
    public List<NPC_SnakeHunter> Hunters { get; set; } = new();

    

    public IntPtr TexEnemy;

    public void Init()
    {
        _world = new SnakeGrid(240, 240, SnakeTerrain.Grass);
        _particleSys = new ParticleSystem(5000);

        ResetGameLogic();

        for (var x = 0; x < VIEW_W; x++)
        {
            for (var y = 0; y < VIEW_H; y++)
            {
                _view[x, y] = SceneFactory.CreateQuad(0, 0, _cellSize, _cellSize, 1f, 1f, 1f, layer: 0);
                var transform = _view[x, y].GetComponent<TransformComponent>();
                transform.Anchor = (0.5f, 0.5f);
                transform.Layer = 0;
            }
        }
        var textureId = NativeMethods.Resources_LoadTexture("Debug", "textures/debug.png");
        _snake.Initialize();

        TexEnemy = NativeMethods.Resources_LoadTexture("Enemy", "Game/Resources/Textures/debug.png");

        _score = UIText.Create("SCORE: 0", SCORE_FONT_SIZE, -15.0f, 7.5f);
        _score.IsVisible(true);
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
        Events.Clear();

        for (var x = 0; x < VIEW_W; x++)
            for (var y = 0; y < VIEW_H; y++)
                _view[x, y].Destroy();

        foreach (var h in Hunters)
        {
            h.Entity.Destroy();
            Hunters.Clear();
        }
    }

    private void ResetGameLogic(int? seed = null)
    {
        if (seed.HasValue) _seed = seed.Value;
        else _seed = new Random().Next();
        Rng = new Random(_seed);

        foreach (var h in Hunters)
        {
            h.Entity.Destroy();
        }
        Hunters.Clear();

        _rng = new Random(_seed);
        Logger.Info($"Seed: {_seed}");

        var gen = new WorldGenerator(_seed);
        gen.Generate(_world);
        var worldW = _world.Width();
        var worldH = _world.Height();

        _foodMap = new FoodType[worldW, worldH];
        _foodCount = 0;
        _currentScore = 0;

        _accum = 0f;
        _time = 0f;
        _tick = TICK_NORMAL;
        _snake.IsSprinting = false;
        _speedBoostTimer = 0f;

        var center = new Vec2i(worldW / 2, worldH / 2);
        var (start, dir) = FindSafeStart(center, 60, START_FORWARD_CLEAR);

        _snake.Clear();
        _snake.Add(start);
        _snake.Add(new Vec2i(Wrap(start.X - dir.X, worldW), Wrap(start.Y - dir.Y, worldH)));
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
            NPC_SnakeHunter.HandleUpdateBehaviour(this, dt);

            var headTile = _world[_snake[0].X, _snake[0].Y].Type;
            var speedMultiplier = 1.0f;

            if (headTile == SnakeTerrain.Mud) speedMultiplier = 1.8f;
            if (headTile == SnakeTerrain.Speed) speedMultiplier = 0.5f;

            var effectivelySprinting = _snake.IsSprinting || _speedBoostTimer > 0;
            var baseTick = effectivelySprinting ? TICK_SPRINT : TICK_NORMAL;

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
            if (Input.GetKeyDown(Keycode.SPACE))
            {
                ResetGameLogic();
            }

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
        var interp = _accum / _tick;

        // FIX: Cast Vec2i to Vec2 before scalar multiplication
        var target = _snake[0] + ((Vec2)_snake.Direction * interp);

        // Calculate wrapped distance vector
        var d = target - _cam;

        // Handle toroidal wrapping for smoothing
        if (d.X > _world.Width() * 0.5f) d.X -= _world.Width();
        else if (d.X < -_world.Width() * 0.5f) d.X += _world.Width();

        if (d.Y > _world.Height() * 0.5f) d.Y -= _world.Height();
        else if (d.Y < -_world.Height() * 0.5f) d.Y += _world.Height();

        var smoothSpeed = Math.Clamp(dt * 10.0f, 0f, 1f);

        _cam += d * smoothSpeed;

        // Wrap camera position cleanly
        _cam.X = (_cam.X % _world.Width() + _world.Width()) % _world.Width();
        _cam.Y = (_cam.Y % _world.Height() + _world.Height()) % _world.Height();

        Render(interp);
    }

    private (Vec2i head, Vec2i dir) FindSafeStart(Vec2i center, int maxRadius = 60, int requiredForwardClear = 3)
    {
        var dirs = new[] { new Vec2i(1, 0), new Vec2i(0, 1), new Vec2i(-1, 0), new Vec2i(0, -1) };
        for (var r = 0; r <= maxRadius; r++)
        {
            for (var dx = -r; dx <= r; dx++)
            {
                for (var dy = -r; dy <= r; dy++)
                {
                    if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;

                    var x = Wrap(center.X + dx, _world.Width());
                    var y = Wrap(center.Y + dy, _world.Height());

                    if (_world[x, y].Blocked) continue;

                    foreach (var d in dirs)
                    {
                        var tx = Wrap(x - d.X, _world.Width());
                        var ty = Wrap(y - d.Y, _world.Height());
                        if (_world[tx, ty].Blocked) continue;

                        var ok = true;
                        for (var i = 1; i <= requiredForwardClear; i++)
                        {
                            var checkPos = new Vec2i(x, y) + (d * i);
                            if (_world[Wrap(checkPos.X, _world.Width()), Wrap(checkPos.Y, _world.Height())].Blocked)
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
        var head = _snake[0];
        var nextRaw = head + _snake.Direction;
        var next = new Vec2i(Wrap(nextRaw.X, _world.Width()), Wrap(nextRaw.Y, _world.Height()));

        if (_world[next].Blocked && _snake.IsSprinting)
        {
            //Drill Baby Drill
            if (_snake.IsSprinting)
            {
                _world.Set(next, e =>
                {
                    e.Blocked = false;
                    e.Type = SnakeTerrain.Grass;
                    e.Food = false;
                });
                SpawnExplosion(next, 50, SnakeTile.Palette.COL_ROCK);
            }
        }
        //Next tile blocked or snake collision = death
        if (_world[next].Blocked || IsSnakeAt(next.X, next.Y))
        {
            _snake.Kill(this);
            return;
        }

        _snake.Insert(0, next);

        if (_world[next.X, next.Y].Food)
        {
            _world[next.X, next.Y].Food = false;
            var type = _foodMap[next.X, next.Y];
            _foodMap[next.X, next.Y] = FoodType.None;
            _foodCount--;

            var pCol = SnakeTile.Palette.COL_FOOD_APPLE;
            switch (type)
            {
                case FoodType.Gold: 
                    _snake.Grow += 5; 
                    _currentScore += 5; 
                    _shake = 0.3f; 
                    pCol = SnakeTile.Palette.COL_FOOD_GOLD; 
                    break;
                case FoodType.Plum: 
                    _snake.Grow -= 2; 
                    _currentScore += 2; 
                    _shake = 0.15f; 
                    pCol = SnakeTile.Palette.COL_FOOD_PLUM; 
                    break;
                case FoodType.Chili: 
                    _snake.Grow += 3; 
                    _currentScore += 1; 
                    _speedBoostTimer += 3.0f; 
                    _shake = 0.2f; 
                    pCol = SnakeTile.Palette.COL_FOOD_CHILI; 
                    break;
                case FoodType.Apple: 
                default: 
                    _snake.Grow += 3; 
                    _currentScore += 1; 
                    _shake = 0.15f; 
                    pCol = SnakeTile.Palette.COL_FOOD_APPLE; 
                    break;
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

    public void SpawnFood()
    {
        while (_foodCount < MAX_FOOD)
        {
            var spawned = false;
            for (var i = 0; i < 100; i++)
            {
                var range = 60;
                var rndOffset = new Vec2i(_rng.Next(-range, range), _rng.Next(-range, range));
                var pos = _snake[0] + rndOffset;

                var x = Wrap(pos.X, _world.Width());
                var y = Wrap(pos.Y, _world.Height());

                if (!_world[x, y].Blocked &&
                    !_world[x, y].Food &&
                    _snake.GetBodyIndexFromWorldPosition(x, y) == -1)
                {
                    _world[x, y].Food = true;
                    _foodCount++;
                    spawned = true;
                    var roll = _rng.NextDouble();
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

    public void SpawnExplosion(Vec2 worldPos, int count, Vec3 color)
    {
        // Calculate relative position to camera
        var dx = worldPos.X - _cam.X;
        var dy = worldPos.Y - _cam.Y;

        // Handle wrapping
        if (dx > _world.Width() / 2f) dx -= _world.Width();
        else if (dx < -_world.Width() / 2f) dx += _world.Width();

        if (dy > _world.Height() / 2f) dy -= _world.Height();
        else if (dy < -_world.Height() / 2f) dy += _world.Height();

        var px = dx * _cellSpacing;
        var py = dy * _cellSpacing;

        var props = new ParticleProps();
        props.Position = new Vec2(px, py);
        props.VelocityVariation = new Vec2(2.0f, 2.0f);
        props.ColorBegin = new Color(color.X, color.Y, color.Z, 1.0f);
        props.ColorEnd = new Color(color.X, color.Y, color.Z, 0.0f);
        props.SizeBegin = 0.3f;
        props.SizeEnd = 0.0f;
        props.SizeVariation = 0.1f;
        props.LifeTime = 0.8f;

        for (var i = 0; i < count; i++)
        {
            var angle = (float)_rng.NextDouble() * 6.28f;
            var speed = (float)_rng.NextDouble() * 2.0f + 0.5f;
            props.Velocity = new Vec2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;

            _particleSys.Emit(props);
        }
    }

    private void Render(float interp)
    {
        var camFloor = new Vec2((float)Math.Floor(_cam.X), (float)Math.Floor(_cam.Y));
        var camFrac = _cam - camFloor;

        var shakeVec = new Vec2(((float)_rng.NextDouble() - 0.5f) * _shake, ((float)_rng.NextDouble() - 0.5f) * _shake);

        var headScr = (camFrac * -1.0f + shakeVec) * _cellSpacing;

        var headPos = _snake[0];
        var headTerrain = _world[headPos.X, headPos.Y].Type;

        var snakeBase = (_snake.IsSprinting || _speedBoostTimer > 0) ? PlayerSnake.COL_SNAKE_SPRINT : PlayerSnake.COL_SNAKE;
        var activeSnakeColor = snakeBase;

        if (headTerrain == SnakeTerrain.Ice)
        {
            activeSnakeColor = Vec3.Lerp(snakeBase, SnakeTile.Palette.COL_TINT_ICE, 0.6f);
        }
        else if (headTerrain == SnakeTerrain.Mud)
        {
            activeSnakeColor = Vec3.Lerp(snakeBase, SnakeTile.Palette.COL_TINT_MUD, 0.5f) * 0.7f;
        }

        for (var vx = 0; vx < VIEW_W; vx++)
        {
            for (var vy = 0; vy < VIEW_H; vy++)
            {
                var wx = Wrap((int)camFloor.X - VIEW_W / 2 + vx, _world.Width());
                var wy = Wrap((int)camFloor.Y - VIEW_H / 2 + vy, _world.Height());

                var px = (vx - VIEW_W / 2f - camFrac.X + shakeVec.X) * (_cellSpacing);
                var py = (vy - VIEW_H / 2f - camFrac.Y + shakeVec.Y) * (_cellSpacing);

                var ent = _view[vx, vy];
                var transform = ent.GetComponent<TransformComponent>();
                transform.Position = (px, py);

                var tileCol = _world[wx,wy].GetPalette(_time);

                if (_world[wx, wy].Food)
                {
                    var f = _foodMap[wx, wy];
                    var fCol = SnakeTile.Palette.COL_FOOD_APPLE;
                    if (f == FoodType.Gold)
                    {
                        fCol = SnakeTile.Palette.COL_FOOD_GOLD;
                    }
                    else if (f == FoodType.Plum)
                    {
                        fCol = SnakeTile.Palette.COL_FOOD_PLUM;
                    }
                    else if (f == FoodType.Chili)
                    {
                        fCol = SnakeTile.Palette.COL_FOOD_CHILI;
                    }
                    tileCol = fCol * (0.8f + 0.2f * (float)Math.Sin(_time * 12f));
                }

                var sIdx = _snake.GetBodyIndexFromWorldPosition(wx, wy);
                if (sIdx != -1)
                {
                    var tailFade = (1.0f - (sIdx * 0.02f));
                    var finalSnakeCol = activeSnakeColor * tailFade;
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
            _score.Text($"SCORE: {_currentScore}");
        }
    }

    private void UpdateFoodCompass(Vec2 headScreenPos)
    {
        var nearest = GetNearestFoodPos();
        if (nearest.X != -1)
        {
            var head = _snake[0];

            float dx = nearest.X - head.X;
            if (Math.Abs(dx) > _world.Width() / 2) dx = -Math.Sign(dx) * (_world.Width() - Math.Abs(dx));

            float dy = nearest.Y - head.Y;
            if (Math.Abs(dy) > _world.Height() / 2) dy = -Math.Sign(dy) * (_world.Height() - Math.Abs(dy));

            var dirVec = new Vec2(dx, dy);
            var normalizedDir = dirVec.Normalized();

            var orbitDist = _cellSpacing * 0.9f;

            var compassSprite = _snake.Compass.GetComponent<SpriteComponent>();
            compassSprite.IsVisible = true;
            var compassPos = headScreenPos + normalizedDir * orbitDist;

            var compassTransform = _snake.Compass.GetComponent<TransformComponent>();
            compassTransform.Position = (compassPos.X, compassPos.Y);

            var pulse = 0.5f + 0.5f * (float)Math.Abs(Math.Sin(_time * 10f));

            var cCol = new Vec3(pulse, pulse, 0);
            var ft = _foodMap[nearest.X, nearest.Y];
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
        var forward = headSize * 0.22f;
        var side = headSize * 0.18f;

        var basePos = new Vec2(hpx, hpy);
        // FIX: Cast _dir (Vec2i) to Vec2 before scalar multiply
        var fwdVec = (Vec2)_snake.Direction * forward;
        var sideVec = new Vec2(-_snake.Direction.Y, _snake.Direction.X) * side;

        var eyeSize = headSize * 0.17f;
        var eye0Transform = _snake.Eyes[0].GetComponent<TransformComponent>();
        eye0Transform.Scale = (eyeSize, eyeSize);
        var eye1Transform = _snake.Eyes[1].GetComponent<TransformComponent>();
        eye1Transform.Scale = (eyeSize, eyeSize);

        var p0 = basePos + fwdVec + sideVec;
        var p1 = basePos + fwdVec - sideVec;

        eye0Transform.Position = (p0.X, p0.Y);
        eye1Transform.Position = (p1.X, p1.Y);
    }

    private void HandleInput()
    {
        var headPos = _snake[0];
        var ignore = _world[headPos.X, headPos.Y].Type == SnakeTerrain.Ice;
        _snake.RecieveInput(ignore);
    }

    private Vec2i GetNearestFoodPos()
    {
        var head = _snake[0];
        var bestFood = new Vec2i(-1, -1);
        var minWeightedDist = float.MaxValue;
        for (var x = 0; x < _world.Width(); x++)
        {
            for (var y = 0; y < _world.Height(); y++)
            {
                if (_world[x, y].Food)
                {
                    float dx = x - head.X;
                    if (Math.Abs(dx) > _world.Width() / 2) dx = Math.Sign(dx) * (_world.Width() - Math.Abs(dx));

                    float dy = y - head.Y;
                    if (Math.Abs(dy) > _world.Height() / 2) dy = Math.Sign(dy) * (_world.Height() - Math.Abs(dy));

                    var distVec = new Vec2(dx, dy);
                    var dist = distVec.Length();

                    var priorityBonus = 0f;
                    switch (_foodMap[x, y])
                    {
                        case FoodType.Gold: priorityBonus = 25.0f; break;
                        case FoodType.Chili: priorityBonus = 15.0f; break;
                        case FoodType.Plum: priorityBonus = -5.0f; break;
                    }
                    var weightedDist = dist - priorityBonus;
                    if (weightedDist < minWeightedDist) { minWeightedDist = weightedDist; bestFood = new Vec2i(x, y); }
                }
            }
        }
        return bestFood;
    }

    private bool IsSnakeAt(int x, int y) => _snake.GetBodyIndexFromWorldPosition(x, y) != -1;
    public static int Wrap(int v, int m) => (v % m + m) % m;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            _particleSys?.Dispose();
        }

        _isDisposed = true;
    }
}