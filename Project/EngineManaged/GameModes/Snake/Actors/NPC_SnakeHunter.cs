using EngineManaged.Numeric;
using EngineManaged.Scene;
using GameModes.Dude;
using SlimeCore.GameModes.Snake.World;
using SlimeCore.Source.World.Actors;
using SlimeCore.Source.World.Grid;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.GameModes.Snake.Actors;

public record NPC_SnakeHunter : Actor<SnakeTerrain>
{

    const int SpawnRadius = 30;

    public const int MaxHunters = 50;

    public enum HunterType
    {
        Normal,
        Chonker
    }
    public required Entity Entity { get; set; }

    public HunterType Type { get; set; }

    public static Vec2i SpawnHunter(SnakeGame game, int spawnRadius = SpawnRadius)
    {
        var type = game.Rng.NextDouble() < 0.10 ? HunterType.Chonker : HunterType.Normal;
        var snakePos = game._snake.Body[0];

        var angle = game.Rng.NextDouble() * Math.PI * 2;
        var dist = game.Rng.NextDouble() * spawnRadius;

        var pos = new Vec2i(
            snakePos.X + (int)Math.Round(Math.Cos(angle) * dist),
            snakePos.Y + (int)Math.Round(Math.Sin(angle) * dist)
        );

        var size = type == HunterType.Chonker ? 2.2f : 0.8f;
        var r = type == HunterType.Chonker ? 0.6f : 1.0f;
        var b = type == HunterType.Chonker ? 0.8f : 0.2f;
        var hunter = SceneFactory.CreateQuad(0, 0, size, size, r, 0f, b, layer: 4);

        // Add Physics (Kinematic so we can control position manually but still collide with player)
        hunter.AddComponent<RigidBodyComponent>();
        hunter.AddComponent<BoxColliderComponent>();
        var rb = hunter.GetComponent<RigidBodyComponent>();
        rb.IsKinematic = true;
        var bc = hunter.GetComponent<BoxColliderComponent>();
        bc.Size = (size * 0.8f, size * 0.8f); // Slightly smaller hitbox

        var hunterTransform = hunter.GetComponent<TransformComponent>();
        hunterTransform.Anchor = (0.5f, 0.5f);
        hunterTransform.Position = (pos.X, pos.Y);
        var hunterSprite = hunter.GetComponent<SpriteComponent>();
        hunterSprite.IsVisible = true;

        game.Hunters.Add(new NPC_SnakeHunter { Entity = hunter, Position = pos, Type = type });
        game.SpawnExplosion(pos, 50, new Vec3(1.0f, 0.2f, 0.2f));
        SafeNativeMethods.Engine_Log($"Spawning Hunter with Id: {hunter.Id}");
        return pos;
    }

    public static void HandleUpdateBehaviour(SnakeGame game, float dt)
    {
        game.SpawnTimer -= dt;
        if (game.SpawnTimer <= 0 && game.Hunters.Count < MaxHunters)
        {
            var hunterPos = SpawnHunter(game);
            var snakeDistance = Vec2i.Distance(hunterPos, game._snake.Body[0]);
            SafeNativeMethods.Engine_Log($"Spawning Hunter with Id: {hunterPos}, {snakeDistance} from snake, there are {game.Hunters.Count} hunters");
            var ramp = MathF.Min(1.5f, game._currentScore * 0.02f);
            game.SpawnTimer = (1.2f - ramp) / (game.ChillTimer > 0 ? 0.5f : 1.0f);
            if (game.SpawnTimer < 0.2f) game.SpawnTimer = 0.2f;
        }

        var timeScale = game.ChillTimer > 0 ? 0.3f : 1.0f;

        for (var i = game.Hunters.Count - 1; i >= 0; i--)
        {
            var me = game.Hunters[i];

            // Vectorized Direction
            var toPlayer = me.PathFindToPlayer(game);

            var separation = Vec2.Zero;
            var neighbors = 0;
            var myRad = me.Type == HunterType.Chonker ? 2.5f : 1.2f;

            foreach (var other in game.Hunters)
            {
                if (me == other) continue;

                var diff = me.Position - other.Position;
                var distSq = diff.LengthSquared();

                if (distSq < myRad * myRad && distSq > 0.001f)
                {
                    var pushStrength = me.Type == HunterType.Chonker ? 8.0f : 4.0f;
                    separation += diff.Normalized() * pushStrength;
                    neighbors++;
                }
            }

            var force = toPlayer * 2.0f;
            if (neighbors > 0) force += separation * 1.5f;

            var speed = (me.Type == HunterType.Chonker ? 2.0f : 4.5f) * timeScale;
            me.Position += force.Normalized() * speed * dt;

            var hunterSprite = me.Entity.GetComponent<SpriteComponent>();
            hunterSprite.IsVisible = true;

            var hunterTransform = me.Entity.GetComponent<TransformComponent>();

            var dx = me.Position.X - game._cam.X;
            var dy = me.Position.Y - game._cam.Y;

            if (dx > SnakeGame.WORLD_W / 2f) dx -= SnakeGame.WORLD_W;
            else if (dx < -SnakeGame.WORLD_W / 2f) dx += SnakeGame.WORLD_W;

            if (dy > SnakeGame.WORLD_H / 2f) dy -= SnakeGame.WORLD_H;
            else if (dy < -SnakeGame.WORLD_H / 2f) dy += SnakeGame.WORLD_H;

            hunterTransform.Position = (
                dx * SnakeGame._cellSpacing,
                dy * SnakeGame._cellSpacing
            );

            var pulseSpeed = me.Type == HunterType.Chonker ? 5.0f : 15.0f;
            var baseSize = me.Type == HunterType.Chonker ? 2.2f : 0.8f;
            var pulse = baseSize + 0.1f * MathF.Sin(game._currentScore * pulseSpeed + i);
            hunterTransform.Scale = (pulse, pulse);

            // Distance check with Vector method
            // Increased kill distance slightly to account for physics collision radius preventing overlap
            var killDist = (me.Type == HunterType.Chonker ? 1.5f : 0.7f) * game._snake.HeadSize;

            if (Vec2.Distance(game._snake.Body[0], me.Position) < killDist)
            {
                if (game._snake.IsSprinting)
                {
                    game.Events.OnEnemyKilled?.Invoke(game, me.Position);
                    me.Entity.Destroy();
                    game.Hunters.RemoveAt(i);
                    game._currentScore += 100;
                    game._shake += 0.3f;
                    game.SpawnExplosion(me.Position, 10, new Vec3(1f, 0f, 0f));
                }
                else
                {
                    game._snake.Kill(game);
                }
            }
        }
    }
    
    public Vec2 PathFindToPlayer(SnakeGame game)
    {
        var bestDir = Vec2i.Zero;
        var bestScore = float.MaxValue;

        var dirs = new[]
        {
            new Vec2i(1,0),
            new Vec2i(-1,0),
            new Vec2i(0,1),
            new Vec2i(0,-1)
        };

        foreach (var d in dirs)
        {
            var next = (Vec2i)Position + d;

            var wx = SnakeGame.Wrap(next.X, SnakeGame.WORLD_W);
            var wy = SnakeGame.Wrap(next.Y, SnakeGame.WORLD_H);

            if (game._world[wx, wy].Blocked)
                continue;

            var dist = Vec2i.Distance(next, game._snake.Body[0]);

            if (dist < bestScore)
            {
                bestScore = dist;
                bestDir = d;
            }
        }

        return bestDir.ToVec2().Normalized();
    }
}
