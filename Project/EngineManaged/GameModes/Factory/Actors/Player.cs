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
        HandleConveyor(mode, deltaTime);

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
            if (!CheckCollision(game, Position + new Vec2(move.X, 0)))
            {
                Position += new Vec2(move.X, 0);
            }
            
            // Try Y movement
            if (!CheckCollision(game, Position + new Vec2(0, move.Y)))
            {
                Position += new Vec2(0, move.Y);
            }
        }
    }

    private void HandleConveyor(FactoryGame game, float dt)
    {
        // Check center point for conveyor influence
        int gx = (int)Math.Floor(Position.X);
        int gy = (int)Math.Floor(Position.Y);
        
        if (gx >= 0 && gx < game.World.Width() && gy >= 0 && gy < game.World.Height())
        {
            var tile = game.World[gx, gy];
            if (tile.Structure == FactoryStructure.ConveyorBelt)
            {
                float baseSpeed = 2.0f;
                float conveyorSpeed = baseSpeed * tile.Tier; 
                
                Vec2 flowDir = GetConveyorFlow(tile, Position.X - gx, Position.Y - gy);
                
                var move = flowDir * conveyorSpeed * dt;
                
                // Apply conveyor movement with collision check
                if (!CheckCollision(game, Position + move))
                {
                    Position += move;
                }
            }
        }
    }

    private Vec2 GetConveyorFlow(FactoryTile tile, float lx, float ly)
    {
        Direction outDir = tile.Direction;
        Direction? inDir = null;
        
        // Check inputs from Bitmask to find a turn
        var dirs = new[] { Direction.North, Direction.East, Direction.South, Direction.West };
        foreach(var d in dirs)
        {
            // Check if bit is set (Input FROM d)
            if ((tile.Bitmask & (1 << (int)d)) != 0)
            {
                // If input is not from opposite side, it's a turn
                if (d != outDir.Opposite())
                {
                    inDir = d;
                    break; // Found a turn input
                }
            }
        }
        
        if (inDir == null)
        {
            // Straight
            return GetDirectionVector(outDir);
        }
        
        // Turn Logic
        return GetTurnVector(outDir, inDir.Value, lx, ly);
    }

    private Vec2 GetTurnVector(Direction outDir, Direction inDir, float lx, float ly)
    {
        // Determine Center and Rotation
        float cx = 0, cy = 0;
        bool ccw = false;
        
        if (outDir == Direction.North)
        {
            if (inDir == Direction.West) { cx = 0; cy = 1; ccw = true; }
            else if (inDir == Direction.East) { cx = 1; cy = 1; ccw = false; }
        }
        else if (outDir == Direction.East)
        {
            if (inDir == Direction.North) { cx = 1; cy = 1; ccw = true; }
            else if (inDir == Direction.South) { cx = 1; cy = 0; ccw = false; }
        }
        else if (outDir == Direction.South)
        {
            if (inDir == Direction.East) { cx = 1; cy = 0; ccw = true; }
            else if (inDir == Direction.West) { cx = 0; cy = 0; ccw = false; }
        }
        else if (outDir == Direction.West)
        {
            if (inDir == Direction.South) { cx = 0; cy = 0; ccw = true; }
            else if (inDir == Direction.North) { cx = 0; cy = 1; ccw = false; }
        }
        
        // Vector from center to point
        float dx = lx - cx;
        float dy = ly - cy;
        
        // Tangent
        Vec2 tangent;
        if (ccw)
            tangent = new Vec2(-dy, dx);
        else
            tangent = new Vec2(dy, -dx);
        
        return tangent.Normalized();
    }

    private Vec2 GetDirectionVector(Direction dir)
    {
        return dir switch
        {
            Direction.North => new Vec2(0, 1),
            Direction.East => new Vec2(1, 0),
            Direction.South => new Vec2(0, -1),
            Direction.West => new Vec2(-1, 0),
            _ => new Vec2(0, 0)
        };
    }

    private bool CheckCollision(FactoryGame game, Vec2 pos)
    {
        // Check corners of the player's bounding box
        float halfSize = Size / 2.0f * 0.8f; // Slightly smaller hitbox than visual size for better feel
        
        // Check 4 corners
        if (IsSolid(game, pos.X - halfSize, pos.Y - halfSize)) return true;
        if (IsSolid(game, pos.X + halfSize, pos.Y - halfSize)) return true;
        if (IsSolid(game, pos.X - halfSize, pos.Y + halfSize)) return true;
        if (IsSolid(game, pos.X + halfSize, pos.Y + halfSize)) return true;
        
        return false;
    }

    private bool IsSolid(FactoryGame game, float x, float y)
    {
        int gx = (int)Math.Floor(x);
        int gy = (int)Math.Floor(y);
        
        if (gx < 0 || gx >= game.World.Width() || gy < 0 || gy >= game.World.Height()) return true; // World bounds
        
        var tile = game.World[gx, gy];
        // Miners and Storage are solid. Conveyors and None are not.
        if (tile.Structure == FactoryStructure.Miner || tile.Structure == FactoryStructure.Storage)
        {
            return true;
        }
        
        return false;
    }

    public override void Destroy()
    {
        Entity.Destroy();
    }

    
}
