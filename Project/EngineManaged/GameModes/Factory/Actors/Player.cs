using EngineManaged.Numeric;
using EngineManaged.Scene;
using SlimeCore.GameModes.Factory.Items;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.Source.Input;
using SlimeCore.Source.World.Actors;
using System;
using System.Linq;

namespace SlimeCore.GameModes.Factory.Actors;

public class Player : Actor<FactoryActors, FactoryGame>, IControllable
{
    public override FactoryActors Kind => FactoryActors.Player;

    protected override float ActionInterval => 0.0f;

    public Entity Entity { get; private set; }
    public float Speed { get; set; } = 5.0f;
    public float SprintMultiplier { get; set; } = 2.0f;
    public float Size { get; set; } = 0.5f;

    public Inventory Inventory { get; } = new();
    private float _dropCooldown;

    public Player(Vec2 startPos)
    {
        Position = startPos;
        Entity = SceneFactory.CreateQuad(Position.X, Position.Y, Size, Size, 1.0f, 1.0f, 1.0f, layer: 10);
        var sprite = Entity.GetComponent<SpriteComponent>();
        sprite.TexturePtr = FactoryResources.TexDebug;
        sprite.Color = (1.0f, 1.0f, 1.0f); // White player for now
        
        // Starting items
        var stone = ItemRegistry.Get("stone");
        if (stone != null) Inventory.AddItem(stone, 50);
        
        var iron = ItemRegistry.Get("iron_ore");
        if (iron != null) Inventory.AddItem(iron, 20);
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
        HandleInteraction(mode, deltaTime);
        
        // Use shared physics for conveyor logic
        var pos = Position;
        FactoryPhysics.ApplyConveyorMovement(mode, ref pos, deltaTime, Size, false);
        Position = pos;

        var transform = Entity.GetComponent<TransformComponent>();
        transform.Position = (Position.X, Position.Y);
        return true;
    }

    public override bool Tick(FactoryGame mode, float deltaTime)
    {
        return true;
    }

    private void HandleInteraction(FactoryGame mode, float dt)
    {
        // Pickup items
        var items = mode.ActorManager.ByType(FactoryActors.DroppedItem);
        foreach (var actor in items)
        {
            if (actor is DroppedItem itemActor)
            {
                float dist = (itemActor.Position - Position).Length();
                if (dist < Size + itemActor.Size)
                {
                    if (Inventory.AddItem(itemActor.Item, itemActor.Count))
                    {
                        mode.ActorManager.Remove(itemActor);
                    }
                }
            }
        }

        // Drop items (Q key)
        if (_dropCooldown > 0) _dropCooldown -= dt;
        
        if (Input.GetKeyDown(Keycode.Q) && _dropCooldown <= 0)
        {
            if (Inventory.Slots.Count > 0)
            {
                var slot = Inventory.Slots[0];
                // Drop 1 item
                var dropped = new DroppedItem(Position + new Vec2(0, -1.0f), slot.Item, 1); // Drop slightly below
                mode.ActorManager.Register(dropped);
                
                Inventory.RemoveItem(slot.Item.Id, 1);
                _dropCooldown = 0.2f;
            }
        }
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
            float currentSpeed = Speed;
            if (Input.GetKeyDown(Keycode.LEFT_SHIFT) || Input.GetKeyDown(Keycode.RIGHT_SHIFT))
            {
                currentSpeed *= SprintMultiplier;
            }

            move = move.Normalized() * currentSpeed * dt;
            
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
