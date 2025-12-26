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

    const int spawnRadius = 30;

    public enum HunterType
    {
        Normal,
        Chonker
    }
    public Entity? HunterEntity { get; set; }

    public HunterType Type { get; set; }

    public static Vec2i SpawnHunter(SnakeGame game)
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
        var ent = game.CreateSpriteEntity(pos.X, pos.Y, size, size, r, 0f, b, 5, game.TexEnemy);

        // Add Physics (Kinematic so we can control position manually but still collide with player)
        ent.AddComponent<RigidBodyComponent>();
        ent.AddComponent<BoxColliderComponent>();
        var rb = ent.GetComponent<RigidBodyComponent>();
        rb.IsKinematic = true;
        var bc = ent.GetComponent<BoxColliderComponent>();
        bc.Size = (size * 0.8f, size * 0.8f); // Slightly smaller hitbox

        var hunterTransform = ent.GetComponent<TransformComponent>();
        hunterTransform.Anchor = (0.5f, 0.5f);
        hunterTransform.Layer = 1;
        game.Hunters.Add(new NPC_SnakeHunter { HunterEntity = ent, Position = pos, Type = type });
        game.SpawnExplosion(pos, 50, new Vec3(1.0f, 0.2f, 0.2f));
        return pos;
    }

    public static void HandleUpdateBehaviour(SnakeGame game, float dt)
    {
        game.SpawnTimer -= dt;
        if (game.SpawnTimer <= 0)
        {
            var hunterPos = SpawnHunter(game);
            var snakeDistance = Vec2i.Distance(hunterPos, game._snake.Body[0]);
            SafeNativeMethods.Engine_Log($"Spawning Hunter {snakeDistance} from snake, there are {game.Hunters.Count} hunters");
            var ramp = MathF.Min(1.5f, game._currentScore * 0.02f);
            game.SpawnTimer = (1.2f - ramp) / (game.ChillTimer > 0 ? 0.5f : 1.0f);
            if (game.SpawnTimer < 0.2f) game.SpawnTimer = 0.2f;
        }

        var timeScale = game.ChillTimer > 0 ? 0.3f : 1.0f;

        for (var i = game.Hunters.Count - 1; i >= 0; i--)
        {
            var me = game.Hunters[i];

            // Vectorized Direction
            var toPlayer = (game._snake.Body[0] - me.Position).Normalized();

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

            var haterTransform = me.HunterEntity.GetComponent<TransformComponent>();
            haterTransform.Position = (me.Position.X, me.Position.Y);

            var pulseSpeed = me.Type == HunterType.Chonker ? 5.0f : 15.0f;
            var baseSize = me.Type == HunterType.Chonker ? 2.2f : 0.8f;
            var pulse = baseSize + 0.1f * MathF.Sin(game._currentScore * pulseSpeed + i);
            haterTransform.Scale = (pulse, pulse);

            // Distance check with Vector method
            // Increased kill distance slightly to account for physics collision radius preventing overlap
            var killDist = (me.Type == HunterType.Chonker ? 1.5f : 0.7f) * SnakeGame._cellSize * 1.3f;

            if (Vec2.Distance(game._snake.Body[0], me.Position) < killDist)
            {
                if (game._snake.IsSprinting)
                {
                    game.Events.OnEnemyKilled?.Invoke(game, me.Position);
                    me.HunterEntity.Destroy();
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
}
