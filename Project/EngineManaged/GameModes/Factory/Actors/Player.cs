using EngineManaged.Numeric;
using EngineManaged.Scene;
using SlimeCore.GameModes.Factory.World;
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
        sprite.TexturePtr = FactoryResources.TexDebug;
        sprite.Color = (1.0f, 1.0f, 1.0f); // White player for now
    }

    public void Update(float dt)
    {
        // Deprecated, logic moved to TakeAction
    }

    public void RecieveInput(bool IgnoreInput)
    {
        throw new NotImplementedException();
    }

    public override bool TakeAction(FactoryGame mode, float deltaTime)
    {
        HandleMovement(mode, deltaTime);
        
        // Use shared physics for conveyor logic
        var pos = Position;
        FactoryPhysics.ApplyConveyorMovement(mode, ref pos, deltaTime, Size);
        Position = pos;

        var transform = Entity.GetComponent<TransformComponent>();
        transform.Position = (Position.X, Position.Y);
        return true;
    }

    private void HandleMovement(FactoryGame game, float dt)
    {
        var move = new Vec2(0, 0);
        if (Input.GetKeyDown(Keycode.W)) move.Y += 1;
        if (Input.GetKeyDown(Keycode.S)) move.Y -= 1;
        if (Input.GetKeyDown(Keycode.A)) move.X -= 1;
        if (Input.GetKeyDown(Keycode.D)) move.X += 1;

        if (move.Length() > 0)
        {
            move = move.Normalized() * Speed * dt;
            
            // Try X movement
            if (!FactoryPhysics.CheckCollision(game, Position + new Vec2(move.X, 0), Size))
            {
                Position += new Vec2(move.X, 0);
            }
            
            // Try Y movement
            if (!FactoryPhysics.CheckCollision(game, Position + new Vec2(0, move.Y), Size))
            {
                Position += new Vec2(0, move.Y);
            }
        }
    }











    public override void Destroy()
    {
        Entity.Destroy();
    }

    
}
