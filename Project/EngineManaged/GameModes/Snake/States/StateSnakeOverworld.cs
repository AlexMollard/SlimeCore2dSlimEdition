using EngineManaged.Numeric;
using EngineManaged.Scene;
using EngineManaged.UI;
using SlimeCore.GameModes.Snake.Actors;
using SlimeCore.GameModes.Snake.World;
using SlimeCore.Source.Core;
using SlimeCore.Source.Input;
using SlimeCore.Source.World.Actors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.GameModes.Snake.States;

public class StateSnakeOverworld : IGameState<SnakeGame>
{
    //Game Logic
    private static float _tick = SnakeGame.TICK_NORMAL;
    private static float _accum;
    private static float _time;


    // UI
    private static UIText _score;
    private static int _scoreCached = -1;
    private const int SCORE_FONT_SIZE = 1;

    private static int _seed;

    // Food
    private static int _foodCount;
    private const int MAX_FOOD = 25;
    
    private enum FoodType { None, Apple, Gold, Plum, Chili }
    private FoodType[][]? _foodMap { get; set; }
    public void Enter(SnakeGame game)
    {
        ResetGameLogic(game);
        var textureId = NativeMethods.Resources_LoadTexture("Debug", "textures/debug.png");
        game._snake.Initialize();
        game._world.Initialize(SnakeGame.VIEW_W, SnakeGame.VIEW_H);

        game.TexEnemy = NativeMethods.Resources_LoadTexture("Enemy", "Game/Resources/Textures/debug.png");

        _score = UIText.Create("SCORE: 0", SCORE_FONT_SIZE, -13.0f, 7.5f);
        _score.IsVisible(true);
        _scoreCached = -1;
    }

    public void Exit(SnakeGame game)
    {
        _score.Destroy();
        game.ActorManager.Destroy();
    }

    public void Update(SnakeGame game, float dt)
    {
        _time += dt;
        if (game._snake.SpeedBoostTimer > 0)
        {
            game._snake.SpeedBoostTimer -= dt;
            if (game._snake.SpeedBoostTimer < 0)
            {
                game._snake.SpeedBoostTimer = 0;
            }
        }

        if (!game._snake.IsDead)
        {
            game.ActorManager.Tick(game, dt);
            var headPos = game._snake[0];
            var ignore = game._world[headPos].Type == SnakeTerrain.Ice;
            game._snake.RecieveInput(ignore);

            NpcSnakeHunter.HandleUpdateBehaviour(game, dt);

            var headTile = game._world[game._snake[0].X, game._snake[0].Y].Type;
            var speedMultiplier = 1.0f;

            if (headTile == SnakeTerrain.Mud) speedMultiplier = 1.8f;
            if (headTile == SnakeTerrain.Speed) speedMultiplier = 0.5f;

            var effectivelySprinting = game._snake.IsSprinting || game._snake.SpeedBoostTimer > 0;
            var baseTick = effectivelySprinting ? SnakeGame.TICK_SPRINT : SnakeGame.TICK_NORMAL;

            _tick = baseTick * speedMultiplier;

            _accum += dt;
            while (_accum >= _tick)
            {
                _accum -= _tick;
                game._snake.Direction = game._snake.NextDirection;
                Step(game);
            }
        }
        else
        {
            if (Input.GetKeyDown(Keycode.SPACE))
            {
                ResetGameLogic(game);
            }

            if (game._snake.Body.Count > 1)
            {
                _accum += dt;
                while (_accum >= _tick)
                {
                    _accum -= _tick;
                    game._snake.RemoveAt(game._snake.Body.Count - 1);
                }
            }
        }

        // Smooth Camera
        var interp = _accum / _tick;

        // FIX: Cast Vec2i to Vec2 before scalar multiplication
        var target = game._snake[0] + ((Vec2)game._snake.Direction * interp);

        // Calculate wrapped distance vector
        var d = target - game._cam;

        // Handle toroidal wrapping for smoothing
        if (d.X > game._world.Width() * 0.5f) d.X -= game._world.Width();
        else if (d.X < -game._world.Width() * 0.5f) d.X += game._world.Width();

        if (d.Y > game._world.Height() * 0.5f) d.Y -= game._world.Height();
        else if (d.Y < -game._world.Height() * 0.5f) d.Y += game._world.Height();

        var smoothSpeed = Math.Clamp(dt * 10.0f, 0f, 1f);

        game._cam += d * smoothSpeed;

        // Wrap camera position cleanly
        game._cam.X = (game._cam.X % game._world.Width() + game._world.Width()) % game._world.Width();
        game._cam.Y = (game._cam.Y % game._world.Height() + game._world.Height()) % game._world.Height();

        Render(game, interp);
    }
    
    

    private void Render(SnakeGame game, float interp)
    {
        var camFloor = new Vec2((float)Math.Floor(game._cam.X), (float)Math.Floor(game._cam.Y));
        var camFrac = game._cam - camFloor;

        var shakeVec = new Vec2(((float)game.Rng.NextDouble() - 0.5f) * game._shake, ((float)game.Rng.NextDouble() - 0.5f) * game._shake);

        var headScr = (camFrac * -1.0f + shakeVec) * SnakeGame._cellSpacing;

        var headPos = game._snake[0];
        var headTerrain = game._world[headPos.X, headPos.Y].Type;

        var snakeBase = (game._snake.IsSprinting || game._snake.SpeedBoostTimer > 0) ? PlayerSnake.COL_SNAKE_SPRINT : PlayerSnake.COL_SNAKE;
        var activeSnakeColor = snakeBase;

        if (headTerrain == SnakeTerrain.Ice)
        {
            activeSnakeColor = Vec3.Lerp(snakeBase, SnakeTile.Palette.COL_TINT_ICE, 0.6f);
        }
        else if (headTerrain == SnakeTerrain.Mud)
        {
            activeSnakeColor = Vec3.Lerp(snakeBase, SnakeTile.Palette.COL_TINT_MUD, 0.5f) * 0.7f;
        }

        for (var vx = 0; vx < SnakeGame.VIEW_W; vx++)
        {
            for (var vy = 0; vy < SnakeGame.VIEW_H; vy++)
            {
                var wx = SnakeGame.Wrap((int)camFloor.X - SnakeGame.VIEW_W / 2 + vx, game._world.Width());
                var wy = SnakeGame.Wrap((int)camFloor.Y - SnakeGame.VIEW_H / 2 + vy, game._world.Height());

                var px = (vx - SnakeGame.VIEW_W / 2f - camFrac.X + shakeVec.X) * (SnakeGame._cellSpacing);
                var py = (vy - SnakeGame.VIEW_H / 2f - camFrac.Y + shakeVec.Y) * (SnakeGame._cellSpacing);

                var ent = game._world.GridRenders[vx][vy];
                var transform = ent.GetComponent<TransformComponent>();
                transform.Position = (px, py);

                var tileCol = game._world[wx, wy].GetPalette(_time);

                if (game._world[wx, wy].Food)
                {
                    var f = _foodMap[wx][wy];
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

                var sIdx = game._snake.GetBodyIndexFromWorldPosition(wx, wy);
                if (sIdx != -1)
                {
                    var tailFade = (1.0f - (sIdx * 0.02f));
                    var finalSnakeCol = activeSnakeColor * tailFade;
                    if (game._snake.IsDead) finalSnakeCol = new Vec3(0.4f, 0.1f, 0.1f);

                    if (sIdx == 0)
                    {
                        var headTransform = game._snake.Head.GetComponent<TransformComponent>();
                        headTransform.Scale = (game._snake.HeadSize, game._snake.HeadSize);
                        headTransform.Position = (px, py);

                        var headSprite = game._snake.Head.GetComponent<SpriteComponent>();
                        headSprite.Color = (finalSnakeCol.X, finalSnakeCol.Y, finalSnakeCol.Z);
                        UpdateEyes(game, px, py, game._snake.HeadSize);
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
                finalTransform.Scale = (game._world.Zoom, game._world.Zoom);
            }
        }

        UpdateFoodCompass(game, headScr);

        if (game._currentScore != _scoreCached)
        {
            _scoreCached = game._currentScore;
            _score.Text($"SCORE: {game._currentScore}");
        }
    }
    private void UpdateEyes(SnakeGame game, float hpx, float hpy, float headSize)
    {
        var forward = headSize * 0.22f;
        var side = headSize * 0.18f;

        var basePos = new Vec2(hpx, hpy);
        // FIX: Cast _dir (Vec2i) to Vec2 before scalar multiply
        var fwdVec = (Vec2)game._snake.Direction * forward;
        var sideVec = new Vec2(-game._snake.Direction.Y, game._snake.Direction.X) * side;

        var eyeSize = headSize * 0.17f;
        var eye0Transform = game._snake.Eyes[0].GetComponent<TransformComponent>();
        eye0Transform.Scale = (eyeSize, eyeSize);
        var eye1Transform = game._snake.Eyes[1].GetComponent<TransformComponent>();
        eye1Transform.Scale = (eyeSize, eyeSize);

        var p0 = basePos + fwdVec + sideVec;
        var p1 = basePos + fwdVec - sideVec;

        eye0Transform.Position = (p0.X, p0.Y);
        eye1Transform.Position = (p1.X, p1.Y);
    }

    private Vec2i GetNearestFoodPos(SnakeGame game)
    {
        var head = game._snake[0];
        var bestFood = new Vec2i(-1, -1);
        var minWeightedDist = float.MaxValue;
        for (var x = 0; x < game._world.Width(); x++)
        {
            for (var y = 0; y < game._world.Height(); y++)
            {
                if (game._world[x, y].Food)
                {
                    float dx = x - head.X;
                    if (Math.Abs(dx) > game._world.Width() / 2) dx = Math.Sign(dx) * (game._world.Width() - Math.Abs(dx));

                    float dy = y - head.Y;
                    if (Math.Abs(dy) > game._world.Height() / 2) dy = Math.Sign(dy) * (game._world.Height() - Math.Abs(dy));

                    var distVec = new Vec2(dx, dy);
                    var dist = distVec.Length();

                    var priorityBonus = 0f;
                    switch (_foodMap[x][y])
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

    private void Step(SnakeGame game)
    {
        var head = game._snake[0];
        var nextRaw = head + game._snake.Direction;
        var next = new Vec2i(SnakeGame.Wrap(nextRaw.X, game._world.Width()), SnakeGame.Wrap(nextRaw.Y, game._world.Height()));

        if (game._world[next].Blocked && game._snake.IsSprinting)
        {
            //Drill Baby Drill
            if (game._snake.IsSprinting)
            {
                game._world.Set(next, e =>
                {
                    e.Blocked = false;
                    e.Type = SnakeTerrain.Grass;
                    e.Food = false;
                });
                game.SpawnExplosion(next, 50, SnakeTile.Palette.COL_ROCK);
            }
        }
        //Next tile blocked or snake collision = death
        if (game._world[next].Blocked || game.IsSnakeAt(next.X, next.Y))
        {
            game._snake.Kill(game);
            return;
        }

        game._snake.Insert(0, next);

        if (game._world[next.X, next.Y].Food)
        {
            game._world[next.X, next.Y].Food = false;
            var type = _foodMap[next.X][next.Y];
            _foodMap[next.X][next.Y] = FoodType.None;
            _foodCount--;

            var pCol = SnakeTile.Palette.COL_FOOD_APPLE;
            switch (type)
            {
                case FoodType.Gold:
                    game._snake.Grow += 5;
                    game._currentScore += 5;
                    game._shake = 0.3f;
                    pCol = SnakeTile.Palette.COL_FOOD_GOLD;
                    break;
                case FoodType.Plum:
                    game._snake.Grow -= 2;
                    game._currentScore += 2;
                    game._shake = 0.15f;
                    pCol = SnakeTile.Palette.COL_FOOD_PLUM;
                    break;
                case FoodType.Chili:
                    game._snake.Grow += 3;
                    game._currentScore += 1;
                    game._snake.SpeedBoostTimer += 3.0f;
                    game._shake = 0.2f;
                    pCol = SnakeTile.Palette.COL_FOOD_CHILI;
                    break;
                case FoodType.Apple:
                default:
                    game._snake.Grow += 3;
                    game._currentScore += 1;
                    game._shake = 0.15f;
                    pCol = SnakeTile.Palette.COL_FOOD_APPLE;
                    break;
            }
            game.SpawnExplosion(next, 15, pCol);
            SpawnFood(game);
        }

        if (game._snake.Grow > 0)
        {
            game._snake.Grow--;
        }
        else if (game._snake.Grow < 0)
        {
            if (game._snake.Body.Count > 1)
            {
                game._snake.RemoveAt(game._snake.Body.Count - 1);
            }
            if (game._snake.Body.Count > 1)
            {
                game._snake.RemoveAt(game._snake.Body.Count - 1);
            }
            game._snake.Grow++;
        }
        else if (game._snake.Body.Count > 0)
        {
            game._snake.RemoveAt(game._snake.Body.Count - 1);
        }

        if (game._snake.Body.Count == 0)
        {
            game._snake.Add(next);
        }
    }

    private void UpdateFoodCompass(SnakeGame game, Vec2 headScreenPos)
    {
        var nearest = GetNearestFoodPos(game);
        if (nearest.X != -1)
        {
            var head = game._snake[0];

            float dx = nearest.X - head.X;
            if (Math.Abs(dx) > game._world.Width() / 2) dx = -Math.Sign(dx) * (game._world.Width() - Math.Abs(dx));

            float dy = nearest.Y - head.Y;
            if (Math.Abs(dy) > game._world.Height() / 2) dy = -Math.Sign(dy) * (game._world.Height() - Math.Abs(dy));

            var dirVec = new Vec2(dx, dy);
            var normalizedDir = dirVec.Normalized();

            var orbitDist = SnakeGame._cellSpacing * 0.9f;

            var compassSprite = game._snake.Compass.GetComponent<SpriteComponent>();
            compassSprite.IsVisible = true;
            var compassPos = headScreenPos + normalizedDir * orbitDist;

            var compassTransform = game._snake.Compass.GetComponent<TransformComponent>();
            compassTransform.Position = (compassPos.X, compassPos.Y);

            var pulse = 0.5f + 0.5f * (float)Math.Abs(Math.Sin(_time * 10f));

            var cCol = new Vec3(pulse, pulse, 0);
            var ft = _foodMap[nearest.X][nearest.Y];
            if (ft == FoodType.Plum) cCol = new Vec3(pulse, 0, pulse);
            if (ft == FoodType.Chili) cCol = new Vec3(pulse, 0, 0);
        }
        else
        {
            var compassSprite = game._snake.Compass.GetComponent<SpriteComponent>();
            compassSprite.IsVisible = false;
        }
    }


    private void ResetGameLogic(SnakeGame game, int? seed = null)
    {
        if (seed.HasValue)
        {
            _seed = seed.Value;
        }
        else
        {
            _seed = new Random().Next();
        }
        game.Rng = new Random(_seed);

        game.ActorManager.Destroy();

        Logger.Info($"Seed: {_seed}");

        var gen = new WorldGenerator(_seed);
        gen.Generate(game._world);
        var worldW = game._world.Width();
        var worldH = game._world.Height();

        _foodMap = new FoodType[worldW][];
        for (var x = 0; x < worldW; x++)
        {
            _foodMap[x] = new FoodType[worldH];
        }

        _foodCount = 0;
        game._currentScore = 0;

        _accum = 0f;
        _time = 0f;
        _tick = SnakeGame.TICK_NORMAL;
        game._snake.Reset(game);
        SpawnFood(game);
    }

    public void SpawnFood(SnakeGame game)
    {
        while (_foodCount < MAX_FOOD)
        {
            var spawned = false;
            for (var i = 0; i < 100; i++)
            {
                var range = 60;
                var rndOffset = new Vec2i(game.Rng.Next(-range, range), game.Rng.Next(-range, range));
                var pos = game._snake[0] + rndOffset;

                var x = SnakeGame.Wrap(pos.X, game._world.Width());
                var y = SnakeGame.Wrap(pos.Y, game._world.Height());

                if (!game._world[x, y].Blocked &&
                    !game._world[x, y].Food &&
                    game._snake.GetBodyIndexFromWorldPosition(x, y) == -1)
                {
                    game._world[x, y].Food = true;
                    _foodCount++;
                    spawned = true;
                    var roll = game.Rng.NextDouble();
                    if (roll < 0.10) _foodMap[x][y] = FoodType.Gold;
                    else if (roll < 0.20) _foodMap[x][y] = FoodType.Chili;
                    else if (roll < 0.35) _foodMap[x][y] = FoodType.Plum;
                    else _foodMap[x][y] = FoodType.Apple;
                    break;
                }
            }
            if (!spawned) break;
        }
    }
}
