using EngineManaged.Numeric;
using EngineManaged.Scene;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.Source.Core;
using SlimeCore.Source.Input;
using SlimeCore.Source.World.Actors;
using System;

namespace SlimeCore.GameModes.Factory.Actors;

public class Player : Actor<FactoryActors, FactoryGame>, IControllable
{
    public override FactoryActors Kind => FactoryActors.Player;

    protected override float ActionInterval => 0.0f;

    public Entity Entity { get; private set; }
    public float Speed { get; set; } = 5.0f;
    public float Size { get; set; } = 0.5f;

    public Player(Vec2 startPos)
    {
        Position = startPos;
        Entity = SceneFactory.CreateQuad(Position.X, Position.Y, Size, Size, 1.0f, 1.0f, 1.0f, layer: 10);
        var sprite = Entity.GetComponent<SpriteComponent>();
        // Load a texture if available, otherwise use a color
        // sprite.Texture = ...
        sprite.Color = (1.0f, 1.0f, 1.0f); // White player for now
    }

    private void HandleInput(float dt)
    {
        var move = new Vec2(0, 0);
        if (Input.GetKeyDown(Keycode.W)) move.Y += 1;
        if (Input.GetKeyDown(Keycode.S)) move.Y -= 1;
        if (Input.GetKeyDown(Keycode.A)) move.X -= 1;
        if (Input.GetKeyDown(Keycode.D)) move.X += 1;

        if (move.Length() > 0)
        {
            move = move.Normalized();
            Position += move * Speed * dt;
        }
    }
    public void RecieveInput(bool IgnoreInput)
    {
        throw new NotImplementedException();
    }

    public override bool TakeAction(FactoryGame mode, float deltaTime)
    {
        HandleInput(deltaTime);

        var transform = Entity.GetComponent<TransformComponent>();
        transform.Position = (Position.X, Position.Y);
        return true;
    }

    public override void Destroy()
    {
        Entity.Destroy();
    }

    
}
