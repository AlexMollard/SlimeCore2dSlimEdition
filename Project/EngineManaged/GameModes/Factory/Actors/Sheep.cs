using EngineManaged.Numeric;
using EngineManaged.Scene;
using SlimeCore.Source.World.Actors;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.GameModes.Factory.Actors;

public class Sheep : Actor<FactoryActors, FactoryGame>
{
    public override FactoryActors Kind => FactoryActors.Animals;
    protected override float ActionInterval => 0.5f;

    public Entity Entity { get; private set; }

    public float Speed { get; set; } = 4.5f;
    public float Size { get; set; } = 0.5f;

    private Vec2 _velocity;
    private Vec2 _targetDir;
    private float _decisionTimer;
    private float _pauseTimer;
    private float _bobTime;

    public Sheep(Vec2 startPos)
    {
        Position = startPos;
        Entity = SceneFactory.CreateQuad(Position.X, Position.Y, Size, Size, 1.0f, 1.0f, 1.0f, layer: 9);
        var sprite = Entity.GetComponent<SpriteComponent>();
        sprite.TexturePtr = FactoryResources.TexSheep;
        sprite.Color = (1.0f, 1.0f, 1.0f); // White player for now
    }

    public override bool TakeAction(FactoryGame mode, float deltaTime)
    {
        _bobTime += deltaTime;

        // Pause, if the sheep wills it
        if (_pauseTimer > 0f)
        {
            _pauseTimer -= deltaTime;
            _velocity *= 0.9f; // gentle settling
        }
        else
        {
            _decisionTimer -= deltaTime;

            // Time to reconsider life choices
            if (_decisionTimer <= 0f)
            {
                _decisionTimer = Random.Shared.NextSingle() * 3f + 1f;

                if (Random.Shared.NextSingle() < 0.35f)
                {
                    _pauseTimer = Random.Shared.NextSingle() * 2f;
                    _targetDir = Vec2.Zero;
                }
                else
                {
                    _targetDir = new Vec2(
                        Random.Shared.NextSingle() * 2f - 1f,
                        Random.Shared.NextSingle() * 2f - 1f
                    ).Normalized();
                }
            }

            // Ease velocity toward intent
            _velocity = Vec2.Lerp(
                _velocity,
                _targetDir * Speed,
                deltaTime * 2.5f
            );
        }

        Position += _velocity * deltaTime;

        var bob = MathF.Sin(_bobTime * 6f) * 0.05f;

        var transform = Entity.GetComponent<TransformComponent>();
        transform.Position = (Position.X, Position.Y + bob);

        return true;
    }

    public override void Destroy()
    {
        Entity.Destroy();
    }

}
