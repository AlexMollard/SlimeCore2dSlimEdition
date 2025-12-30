using EngineManaged.Numeric;
using EngineManaged.Scene;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.Source.World.Actors;
using SlimeCore.Source.World.Actors.Interfaces;
using SlimeCore.Source.World.Grid.Pathfinding;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.GameModes.Factory.Actors;

public class Wolf : Actor<FactoryActors, FactoryGame>, IThreat
{
    public override FactoryActors Kind => FactoryActors.Animals;
    protected override float ActionInterval => 0.5f;

    public Entity Entity { get; private set; }

    public float Speed { get; set; } = 3.5f;
    public float Size { get; set; } = 0.5f;

    public float Radius { get; } = 0.6f;

    private Vec2 _velocity;
    private Vec2 _targetDir;
    private float _decisionTimer;
    private float _pauseTimer;
    private float _bobTime;
    private float _hunger;

    private readonly PathFollower<TileAStarPlanner> _pathFollower;
    private Sheep? _target;


    public Wolf(Vec2 startPos)
    {
        Position = startPos;
        Entity = SceneFactory.CreateQuad(Position.X, Position.Y, Size, Size, 1.0f, 1.0f, 1.0f, layer: 9);
        var sprite = Entity.GetComponent<SpriteComponent>();
        sprite.TexturePtr = FactoryResources.TexWolf;
        _pathFollower = new PathFollower<TileAStarPlanner>();
        //sprite.Color = (1.0f, 1.0f, 1.0f); // White player for now
    }

    public override bool TakeAction(FactoryGame mode, float deltaTime)
    {
        _bobTime += deltaTime;
        _hunger -= deltaTime * 2f; // get hungry over time

        // Pause, if the sheep wills it
        if (_pauseTimer > 0f)
        {
            _pauseTimer -= deltaTime;
            _velocity *= 0.9f; // gentle settling
        }
        else
        {
            // Decide WHO to hunt (rarely)
            _decisionTimer -= deltaTime;
            if (_decisionTimer <= 0f)
            {
                _decisionTimer = mode.Rng.NextSingle() * 3f + 1f;

                if (_target == null || !_target.IsAlive)
                {
                    _target = FindClosestSheep(mode);
                }
            }

            if (_target != null)
            {
                var nextDir = _pathFollower.Update(Position, _target.Position.ToVec2Int(), mode.World);
                _targetDir = nextDir;
            }
            else
            {
                _targetDir = Vec2.Zero;
            }
            _velocity = Vec2.Lerp(
                _velocity,
                _targetDir * Speed,
                deltaTime * 2.5f
            );
        }

        // Apply movement with collision
        var move = _velocity * deltaTime;
        if (move.Length() > 0)
        {
            // Try X movement
            if (!FactoryPhysics.CheckCollision(mode, Position + new Vec2(move.X, 0), Size))
            {
                Position += new Vec2(move.X, 0);
            }
            else
            {
                _velocity.X = -_velocity.X * 0.5f; // Bounce
                _targetDir.X = -_targetDir.X;
            }

            // Try Y movement
            if (!FactoryPhysics.CheckCollision(mode, Position + new Vec2(0, move.Y), Size))
            {
                Position += new Vec2(0, move.Y);
            }
            else
            {
                _velocity.Y = -_velocity.Y * 0.5f; // Bounce
                _targetDir.Y = -_targetDir.Y;
            }
        }

        // Apply Conveyor Physics
        var pos = Position;
        FactoryPhysics.ApplyConveyorMovement(mode, ref pos, deltaTime, Size);
        Position = pos;

        float bob = MathF.Sin(_bobTime * 6f) * 0.05f;

        var transform = Entity.GetComponent<TransformComponent>();
        transform.Position = (Position.X, Position.Y + bob);

        return true;
    }

    public override void Destroy()
    {
        Entity.Destroy();
    }

    public static void Populate(FactoryGame game, int amount)
    {
        var coords = new Vec2i(game.Rng!.Next(game.World.Width()), game.Rng!.Next(game.World.Width()));
        int counter = 0;
        for (int i = 0; i < amount; i++)
        {
            game.ActorManager?.Register(new Wolf(coords));
            counter += game.Rng.Next(10);
            if (counter % 2 == 0)
            {
                coords = new Vec2i(game.Rng!.Next(game.World.Width()), game.Rng!.Next(game.World.Width()));
            }
        }
    }

    private Sheep? FindClosestSheep(FactoryGame mode)
    {
        Sheep? best = null;
        float bestDist = float.MaxValue;

        foreach (var actor in mode.ActorManager!.ByType(FactoryActors.Animals))
        {
            if (actor is Sheep sheep)
            {
                float d = (sheep.Position - Position).LengthSquared();
                if (d < bestDist)
                {
                    bestDist = d;
                    best = sheep;
                }
            }
        }

        return best;
    }


    
}

