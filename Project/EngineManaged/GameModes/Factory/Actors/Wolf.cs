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

public class Wolf : Actor<FactoryActors, FactoryGame>, IThreat, IMobileEntity
{
    private FactoryActors Prioritykind { get; set; } = FactoryActors.Animals;
    public override int Priority => ToPriority(Prioritykind);
    public override FactoryActors Kind => FactoryActors.Animals;
    protected override float ActionInterval => 0.5f;

    public Entity Entity { get; private set; }

    public float Speed { get; set; } = 5f;
    public float Size { get; set; } = 0.5f;

    public float Radius { get; } = 0.6f;
    public Vec2 Velocity { get; set; }

    private Vec2 _targetDir;
    private float _decisionTimer;
    private float _bobTime;
    private float _hunger;

    private readonly PathFollower<BasicPlanner> _pathFollower;
    private Sheep? _target;
    private Vec2i _targetPos { get; set; }


    public Wolf(Vec2 startPos)
    {
        Position = startPos;
        Entity = SceneFactory.CreateQuad(Position.X, Position.Y, Size, Size, 1.0f, 1.0f, 1.0f, layer: 9);
        var sprite = Entity.GetComponent<SpriteComponent>();
        sprite.TexturePtr = FactoryResources.TexWolf;
        _pathFollower = new PathFollower<BasicPlanner>();
        //sprite.Color = (1.0f, 1.0f, 1.0f); // White player for now
    }

    public override bool TakeAction(FactoryGame mode, float deltaTime)
    {
        _bobTime += deltaTime;
        _hunger -= deltaTime * 2f; // get hungry over time

        // Decide WHO to hunt (rarely)
        _decisionTimer -= deltaTime;
        if (_decisionTimer <= 0f)
        {
            _decisionTimer = mode.Rng.NextSingle() * 3f + 1f;

            if (_target == null || !_target.IsAlive)
            {
                _target = FindClosestSheep(mode);
                _targetPos = _target.Position.ToVec2Int();
            }
        }

        if (_target != null)
        {
            var nextDir = _pathFollower.Update(Position, _targetPos, mode.World);
            _targetDir = nextDir;
        }
        else
        {
            _targetDir = Vec2.Zero;
        }
        Velocity = Vec2.Lerp(
            Velocity,
            _targetDir * Speed,
            deltaTime * 2.5f
        );
        

        // Apply movement with collision
        var move = Velocity * deltaTime;
        var newVelocity = Velocity;
        if (move.Length() > 0)
        {
            // Try X movement
            if (!FactoryPhysics.CheckCollision(mode, Position + new Vec2(move.X, 0), Size))
            {
                Position += new Vec2(move.X, 0);
            }
            else
            {
                newVelocity.X = -Velocity.X * 0.5f; // Bounce
            }

            // Try Y movement
            if (!FactoryPhysics.CheckCollision(mode, Position + new Vec2(0, move.Y), Size))
            {
                Position += new Vec2(0, move.Y);
            }
            else
            {
                newVelocity.Y = -Velocity.Y * 0.5f; // Bounce
            }
        }
        Velocity = newVelocity;

        // Apply Conveyor Physics
        var pos = Position;
        FactoryPhysics.ApplyConveyorMovement(mode, ref pos, deltaTime, Size);
        Position = pos;

        float bob = MathF.Sin(_bobTime * 6f) * 0.05f;

        var transform = Entity.GetComponent<TransformComponent>();
        transform.Position = (Position.X, Position.Y + bob);

        Hunt(mode);
        return true;
    }

    public override bool Tick(FactoryGame mode, float deltaTime)
    {
        //Wolf has no requisite behaviour on Tick
        if (mode.InView(Position))
        {
            Prioritykind = FactoryActors.OnScreenEntity;
        }
        else
        {
            Prioritykind = FactoryActors.Animals;
        }
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

    private void Hunt(FactoryGame mode)
    {
        if (_targetPos == null || _target == null)
        {
            return;
        }

        var currentTargetPosition = _target.Position.ToVec2Int();
        if (_targetPos.Heuristic(currentTargetPosition) > 20)
        {
            _targetPos = currentTargetPosition;
            Speed = 0.5f; //Wolfy realises sheep is far, slows down
            return;
        }

        float proximity = _targetPos.Heuristic(Position.ToVec2Int());

        if (proximity > 100f)
        {
            Speed = 10f;
        }
        else if (proximity > 50f)
        {
            Speed = 5f;
        }
        else if (proximity > 5f)
        {
            Speed = 20f;
        }


        if (proximity < 0.5f)
        {
            if (_target.Position.Heuristic(Position) > 1f)
            {
                //Recalculate path, wolfy not close enough
                _targetPos = currentTargetPosition;
            }
            else
            {
                _target.Kill(mode);
                _target = null;
            }
        }
    }


    
}

