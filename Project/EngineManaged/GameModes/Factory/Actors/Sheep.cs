using EngineManaged.Numeric;
using EngineManaged.Scene;
using SlimeCore.GameModes.Factory.Items;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.Source.World.Actors;
using SlimeCore.Source.World.Actors.Interfaces;
using SlimeCore.Source.World.Grid.Pathfinding;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.GameModes.Factory.Actors;

public class Sheep : Actor<FactoryActors, FactoryGame>
{
    private FactoryActors Prioritykind { get; set; } = FactoryActors.Animals;
    public override int Priority => ToPriority(Prioritykind);
    public override FactoryActors Kind => FactoryActors.Animals;
    protected override float ActionInterval => 0.5f;

    public Entity Entity { get; private set; }

    public float Speed { get; set; } = 3.5f;
    public float Size { get; set; } = 0.5f;
    public bool IsAlive { get; set; } = true;

    private Vec2 _velocity;
    private Vec2 _targetDir;
    private float _decisionTimer;
    private float _pauseTimer;
    private float _bobTime;
    private float _hunger;

    private readonly TileMemorySteering _memorySteering = new();
    private const float FearRadius = 6f;
    private PathFollower<BasicPlanner>? _grassPath;
    private Vec2i? _grassTarget;



    public Sheep(Vec2 startPos)
    {
        Position = startPos;
        Entity = SceneFactory.CreateQuad(Position.X, Position.Y, Size, Size, 1.0f, 1.0f, 1.0f, layer: 9);
        var sprite = Entity.GetComponent<SpriteComponent>();
        sprite.TexturePtr = FactoryResources.TexSheep;
        //sprite.Color = (1.0f, 1.0f, 1.0f); // White player for now
    }

    public override bool TakeAction(FactoryGame mode, float deltaTime)
    {
        if (!IsAlive) return false;

        _bobTime += deltaTime;
        _hunger -= deltaTime * 2f; // get hungry over time

        // Pause, if the sheep wills it
        if (_pauseTimer > 0f)
        {
            _pauseTimer -= deltaTime;
            _velocity *= 0.9f; // gentle settling
            var position = Position.ToVec2Int();
            if (_hunger < 100f && mode.World[position].Type == FactoryTerrain.Grass)
            {
                mode.World.Set(position, x => x.Type = FactoryTerrain.Dirt);
                _hunger += 20f;
                mode.World.UpdateNeighbors(mode, position);
            }
        }
        else
        {
            _decisionTimer -= deltaTime;

            // Time to reconsider life choices, touch grass
            if (_decisionTimer <= 0f)
            {

                _decisionTimer = mode.Rng.NextSingle() * 3f + 1f;
                var fear = ComputeFear(mode);

                if (fear != Vec2.Zero)
                {
                    _targetDir = fear; // terror overrides whimsy
                }
                else if (mode.Rng.NextSingle() < 0.35f)
                {
                    _pauseTimer = mode.Rng.NextSingle() * 2f;
                    _targetDir = Vec2.Zero;
                }
                else //Whimsical wandering
                {
                    if (_hunger < 50f && _grassTarget == null)
                    {
                        _grassTarget = FindGrass(mode);
                        if (_grassTarget != null)
                        {
                            _grassPath ??= new PathFollower<BasicPlanner>();
                        }
                    }

                    if (_grassTarget != null)
                    {
                        _targetDir = _grassPath!.Update(
                            Position,
                            _grassTarget.Value,
                            mode.World
                        );

                        if (Position.ToVec2Int() == _grassTarget)
                            _grassTarget = null;
                    }
                }
            }
            _targetDir = _memorySteering.ChooseDirection(
                Position,
                _targetDir,
                mode.World,
                deltaTime
            );
            if (_targetDir == Vec2.Zero && _velocity.Length() < 0.1f)
            {
                _targetDir = new Vec2(
                    mode.Rng.NextSingle() * 2f - 1f,
                    mode.Rng.NextSingle() * 2f - 1f
                ).Normalized();
            }

            // Ease velocity toward intent
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

    public override bool Tick(FactoryGame mode, float deltaTime)
    {
        //Sheep has no requisite behaviour on Tick
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

    public void Kill(FactoryGame mode)
    {
        if (!IsAlive) return;
        IsAlive = false;
        
        // Drop Item
        var item = ItemRegistry.Get("mutton");
        if (item != null)
        {
            var dropped = new DroppedItem(Position, item, 1);
            mode.ActorManager?.Register(dropped);
        }

        mode.ActorManager?.Remove(this);
    }

    private Vec2 ComputeFear(FactoryGame mode)
    {
        var fear = Vec2.Zero;

        foreach (var actor in mode.ActorManager!.ByType(FactoryActors.Animals))
        {
            if (actor is IThreat threat)
            {
                var diff = Position - threat.Position;
                float dist = diff.Length();

                if (dist < FearRadius && dist > threat.Radius)
                {
                    fear += diff.Normalized() * (1f - dist / FearRadius);
                }
            }
        }

        return fear.Normalized();
    }

    private Vec2i? FindGrass(FactoryGame mode, int radius = 8)
    {
        var origin = Position.ToVec2Int();

        for (int r = 1; r <= radius; r++)
        for (int dx = -r; dx <= r; dx++)
        for (int dy = -r; dy <= r; dy++)
        {
            var p = origin + new Vec2i(dx, dy);
            if (!mode.World.InBounds(p))
            {
                continue;
            }

            if (mode.World[p].Type == FactoryTerrain.Grass &&
                !mode.World[p].IsBlocked())
            {
                return p;
            }
        }

        return null;
    }


    public static void Populate(FactoryGame game, int amount)
    {
        var coords = new Vec2i(game.Rng!.Next(game.World.Width()), game.Rng!.Next(game.World.Width()));
        int counter = 0;
        for (int i = 0; i < amount; i++)
        {
            game.ActorManager?.Register(new Sheep(coords));
            counter += game.Rng.Next(10);
            if (counter % 2 == 0)
            {
                coords = new Vec2i(game.Rng!.Next(game.World.Width()), game.Rng!.Next(game.World.Width()));
            }
        }
    }
}
