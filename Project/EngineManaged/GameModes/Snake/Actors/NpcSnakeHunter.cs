using EngineManaged.Numeric;
using EngineManaged.Scene;
using SlimeCore.Source.World.Actors;
using System;

namespace SlimeCore.GameModes.Snake.Actors;

public class NpcSnakeHunter : Actor<SnakeActors, SnakeGame>
{

    const int SpawnRadius = 50;

    public const int MaxHunters = 500;

    public enum HunterType
    {
        Normal,
        Chonker
    }
    public required Entity Entity { get; set; }

    public HunterType Type { get; set; }

    public override SnakeActors Kind => SnakeActors.SnakeHunter;

    protected override float ActionInterval =>
        Type == HunterType.Chonker ? 0.35f : 0.15f;

    public static int PulseIterator;

    public static NpcSnakeHunter SpawnHunter(SnakeGame game, int spawnRadius = SpawnRadius)
    {
        var type = game.Rng.NextDouble() < 0.10 ? HunterType.Chonker : HunterType.Normal;
        var snakePos = game._snake.Body[0];

        double angle = game.Rng.NextDouble() * Math.PI * 2;
        double dist = game.Rng.NextDouble() * spawnRadius;

        var pos = new Vec2i(
            snakePos.X + (int)Math.Round(Math.Cos(angle) * dist),
            snakePos.Y + (int)Math.Round(Math.Sin(angle) * dist)
        );

        float size = type == HunterType.Chonker ? 2.2f : 0.8f;
        float r = type == HunterType.Chonker ? 0.6f : 1.0f;
        float b = type == HunterType.Chonker ? 0.8f : 0.2f;
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
        var hunterActor = new NpcSnakeHunter { Entity = hunter, Position = pos, Type = type };

        game.ActorManager.Register(hunterActor);
        game.SpawnExplosion(pos, 50, new Vec3(1.0f, 0.2f, 0.2f));
        SafeNativeMethods.Engine_Log($"Spawning Hunter with Id: {hunter.Id}");
        return hunterActor;
    }

    public static void HandleUpdateBehaviour(SnakeGame game, float dt)
    {
        game.SpawnTimer -= dt;
        int hunterCount = game.ActorManager.Count(SnakeActors.SnakeHunter);
        if (game.SpawnTimer <= 0 && hunterCount < MaxHunters)
        {
            var hunterActor = SpawnHunter(game);
            float snakeDistance = Vec2i.Distance((Vec2i)hunterActor.Position, game._snake.Body[0]);
            SafeNativeMethods.Engine_Log($"Spawning Hunter with Id: {hunterActor.Entity.Id}, {snakeDistance} from snake, there are {hunterCount} hunters");
            float ramp = MathF.Min(1.5f, game._currentScore * 0.02f);
            game.SpawnTimer = (1.2f - ramp) / (game.ChillTimer > 0 ? 0.5f : 1.0f);
            if (game.SpawnTimer < 0.2f)
            {
                game.SpawnTimer = 0.2f;
            }
        }
        SafeNativeMethods.Engine_Log($"there are {hunterCount} active hunters and {game.SpawnTimer} seconds left");

        PulseIterator = 0;
    }

    public Vec2 PathFindToPlayer(SnakeGame game)
    {
        var bestDir = Vec2i.Zero;
        float bestScore = float.MaxValue;

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

            int wx = SnakeGame.Wrap(next.X, game._world.Width());
            int wy = SnakeGame.Wrap(next.Y, game._world.Height());

            if (game._world[wx, wy].Blocked)
                continue;

            float dist = Vec2i.Distance(next, game._snake.Body[0]);

            if (dist < bestScore)
            {
                bestScore = dist;
                bestDir = d;
            }
        }

        return bestDir.ToVec2().Normalized();
    }

    public override bool TakeAction(SnakeGame mode, float deltaTime)
    {
        if (Entity == null)
        {
            return false;
        }
        ActionCooldown -= deltaTime;
        if (ActionCooldown > 0f)
        {
            return false;
        }
        PulseIterator++;
        float timeScale = mode.ChillTimer > 0 ? 0.3f : 1.0f;
        // Vectorized Direction
        var toPlayer = PathFindToPlayer(mode);

        var separation = Vec2.Zero;
        int neighbors = 0;
        float myRad = Type == HunterType.Chonker ? 2.5f : 1.2f;

        foreach (var other in mode.ActorManager.Active(SnakeActors.SnakeHunter))
        {
            if (this == other) continue;

            var diff = Position - other.Position;
            float distSq = diff.LengthSquared();

            if (distSq < myRad * myRad && distSq > 0.001f)
            {
                float pushStrength = Type == HunterType.Chonker ? 8.0f : 4.0f;
                separation += diff.Normalized() * pushStrength;
                neighbors++;
            }
        }

        var force = toPlayer * 2.0f;
        if (neighbors > 0) force += separation * 1.5f;

        float speed = (Type == HunterType.Chonker ? 2.0f : 4.5f) * timeScale;
        Position += force.Normalized() * speed * deltaTime;

        var hunterSprite = Entity.GetComponent<SpriteComponent>();
        hunterSprite.IsVisible = true;

        var hunterTransform = Entity.GetComponent<TransformComponent>();

        float dx = Position.X - mode._cam.X;
        float dy = Position.Y - mode._cam.Y;

        if (dx > mode._world.Width() / 2f) dx -= mode._world.Width();
        else if (dx < -mode._world.Width() / 2f) dx += mode._world.Width();

        if (dy > mode._world.Height() / 2f) dy -= mode._world.Height();
        else if (dy < -mode._world.Height() / 2f) dy += mode._world.Height();

        hunterTransform.Position = (
            dx * SnakeGame._cellSpacing,
            dy * SnakeGame._cellSpacing
        );

        float pulseSpeed = Type == HunterType.Chonker ? 5.0f : 15.0f;
        float baseSize = Type == HunterType.Chonker ? 2.2f : 0.8f;
        float pulse = baseSize + 0.1f * MathF.Sin(mode._currentScore * pulseSpeed + PulseIterator);
        hunterTransform.Scale = (pulse, pulse);

        // Distance check with Vector method
        // Increased kill distance slightly to account for physics collision radius preventing overlap
        float killDist = (Type == HunterType.Chonker ? 2.4f : 1.0f) * mode._snake.HeadSize;

        if (Vec2.Distance(mode._snake.Body[0], Position) < killDist)
        {
            if (mode._snake.IsSprinting)
            {
                mode.Events.OnEnemyKilled?.Invoke(mode, Position);
                mode.ActorManager.Remove(this);
                mode._currentScore += 100;
                mode._shake += 0.3f;
                mode.SpawnExplosion(Position, 10, new Vec3(1f, 0f, 0f));
            }
            else
            {
                mode._snake.Kill(mode);
            }
        }

        return true;
    }

    public override void Destroy()
    {
        if (Entity == null)
        {
            return;
        }
        Entity.Destroy();
        Entity = null;
    }
}
