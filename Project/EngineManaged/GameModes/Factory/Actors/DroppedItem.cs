using EngineManaged.Numeric;
using EngineManaged.Scene;
using SlimeCore.GameModes.Factory.Items;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.Source.Common;
using SlimeCore.Source.Core;
using SlimeCore.Source.World.Actors;
using System;

namespace SlimeCore.GameModes.Factory.Actors;

public class DroppedItem : Actor<FactoryActors, FactoryGame>
{
    public override FactoryActors Kind => FactoryActors.DroppedItem;

    protected override float ActionInterval => 0.0f; // Run every frame

    public Entity Entity { get; private set; }
    public ItemDefinition Item { get; private set; }
    public int Count { get; set; }
    public float Size { get; set; } = 0.25f;
    public Vec2 Velocity { get; set; }
    public float EjectionTimer { get; set; }

    // Bobbing animation
    private float _bobTimer;
    private float _baseY;
    public bool IsDestroyed { get; private set; }
    private Vec2i _lastTile = new(-1, -1);

    public DroppedItem(Vec2 position, ItemDefinition item, int count)
    {
        Position = position;
        Item = item;
        Count = count;
        _baseY = position.Y;

        Entity = SceneFactory.CreateQuad(Position.X, Position.Y, Size, Size, 1.0f, 1.0f, 1.0f, layer: 5);
        var sprite = Entity.GetComponent<SpriteComponent>();
        sprite.TexturePtr = item.IconTexture;
        sprite.Color = (1.0f, 1.0f, 1.0f);
        
        // Add collider for pickup? Or just check distance in Update?
        // Adding a circle collider might be good if the physics engine handles triggers.
        // For now, we'll stick to distance check in Player or here.
    }

    public override bool TakeAction(FactoryGame mode, float deltaTime)
    {
        if (IsDestroyed) return false;

        // Update Tile Registration
        var currentTilePos = new Vec2i((int)Math.Floor(Position.X), (int)Math.Floor(Position.Y));
        if (currentTilePos != _lastTile)
        {
            // Unregister from old
            if (mode.World.InBounds(_lastTile))
            {
                mode.World[_lastTile].Items.Remove(this);
            }
            
            // Register to new
            if (mode.World.InBounds(currentTilePos))
            {
                mode.World[currentTilePos].Items.Add(this);
            }
            _lastTile = currentTilePos;
        }

        // 1. Apply Velocity (for ejection from buildings)
        if (Velocity.LengthSquared() > 0.001f)
        {
            var move = Velocity * deltaTime;
            var target = Position + move;
            // Check collision for velocity movement too
            if (!IsBlockedByItems(mode, target, Velocity.Normalized()))
            {
                Position = target;
            }
            else
            {
                Velocity = Vec2.Zero; // Stop if hit something
            }
        }

        // 2. Apply Conveyor Physics
        var pos = Position;
        // Pass a very small size for collision checks to prevent items getting stuck on walls
        // while transitioning between tiles or if they are slightly offset.
        // Also ignore collision to allow items to enter buildings (which are solid)
        // Pass IsBlockedByItems as extra check
        FactoryPhysics.ApplyConveyorMovement(mode, ref pos, deltaTime, 0.1f, true, (targetPos) => 
        {
            var moveDir = (targetPos - Position).Normalized();
            return IsBlockedByItems(mode, targetPos, moveDir);
        });
        
        // Debug logging
        if ((pos - Position).LengthSquared() > 0)
        {
            // Moved by conveyor
            // Logger.Trace($"Item moved by conveyor. Old: {Position}, New: {pos}");
        }
        else
        {
            int tx = (int)Math.Floor(Position.X);
            int ty = (int)Math.Floor(Position.Y);
            if (mode.World != null && tx >= 0 && tx < mode.World.Width() && ty >= 0 && ty < mode.World.Height())
            {
                var t = mode.World[tx, ty];
                if (t.BuildingId == "conveyor")
                {
                    // Logger.Trace($"Item stuck on belt at {tx},{ty}. Dir: {t.Direction}. Pos: {Position.X:F2},{Position.Y:F2}");
                }
            }
        }

        Position = pos;

        // 3. Friction / Velocity Damping
        // Check if we are on a conveyor
        int gx = currentTilePos.X;
        int gy = currentTilePos.Y;
        bool onConveyor = false;
        if (mode.World.InBounds(gx, gy))
        {
            if (mode.World[gx, gy].BuildingId == "conveyor")
            {
                onConveyor = true;
            }
        }

        if (onConveyor)
        {
            // If on conveyor, dampen velocity quickly so conveyor takes over
            // But don't kill it instantly, allow smooth transition
            Velocity *= (1.0f - deltaTime * 5.0f);
        }
        else
        {
            // Normal friction (e.g. on ground)
            Velocity *= (1.0f - deltaTime * 1.0f);
        }
        
        // Clamp velocity to 0 if very small
        if (Velocity.LengthSquared() < 0.0001f) Velocity = Vec2.Zero;

        // Check if we are on a building that accepts items (like Storage)
        if (EjectionTimer > 0)
        {
            EjectionTimer -= deltaTime;
        }
        else if (mode.BuildingSystem != null && mode.World.InBounds(gx, gy))
        {
            var tile = mode.World[gx, gy];
            // If we are on a non-conveyor structure (like Storage), move towards center to be accepted
            bool isBuilding = !string.IsNullOrEmpty(tile.BuildingId);
            if (isBuilding && tile.BuildingId != "conveyor")
            {
                // Use Item.Id directly
                string itemId = Item.Id;
                
                float cx = gx + 0.5f;
                float cy = gy + 0.5f;
                var toCenter = new Vec2(cx - Position.X, cy - Position.Y);
                float distSq = toCenter.LengthSquared();

                // If close enough, try to insert
                if (distSq < 0.05f) 
                {
                    if (mode.BuildingSystem.TryAcceptItem(gx, gy, itemId))
                    {
                        mode.ActorManager?.Remove(this);
                        return false; // Stop processing
                    }
                }
                else
                {
                    // Move towards center (suction)
                    float speed = 2.0f;
                    var move = toCenter.Normalized() * speed * deltaTime;
                    
                    // Don't overshoot
                    if (move.LengthSquared() > distSq) move = toCenter;

                    var targetPos = Position + move;
                    if (!IsBlockedByItems(mode, targetPos, move.Normalized()))
                    {
                        Position = targetPos;
                    }
                }
            }
        }

        // Bobbing animation
        _bobTimer += deltaTime * 5.0f; // Faster bob
        float bobOffset = (float)Math.Sin(_bobTimer) * 0.1f; // Larger bob
        
        var transform = Entity.GetComponent<TransformComponent>();
        transform.Position = (Position.X, Position.Y + bobOffset);

        return true;
    }

    private bool IsBlockedByItems(FactoryGame mode, Vec2 targetPos, Vec2 moveDir)
    {
        var targetTilePos = new Vec2i((int)Math.Floor(targetPos.X), (int)Math.Floor(targetPos.Y));
        
        // Check center tile
        if (CheckCollisionInTile(mode, targetTilePos, targetPos, moveDir)) return true;

        // Check neighbors
        float collisionDist = Size * 0.95f;
        float checkDist = collisionDist + 0.1f; // Buffer to ensure we check neighbors when close to edge

        float relX = targetPos.X - targetTilePos.X;
        float relY = targetPos.Y - targetTilePos.Y;

        // Left
        if (relX < checkDist)
        {
            if (CheckCollisionInTile(mode, new Vec2i(targetTilePos.X - 1, targetTilePos.Y), targetPos, moveDir)) return true;
            if (relY < checkDist && CheckCollisionInTile(mode, new Vec2i(targetTilePos.X - 1, targetTilePos.Y - 1), targetPos, moveDir)) return true;
            if (relY > 1.0f - checkDist && CheckCollisionInTile(mode, new Vec2i(targetTilePos.X - 1, targetTilePos.Y + 1), targetPos, moveDir)) return true;
        }
        // Right
        else if (relX > 1.0f - checkDist)
        {
            if (CheckCollisionInTile(mode, new Vec2i(targetTilePos.X + 1, targetTilePos.Y), targetPos, moveDir)) return true;
            if (relY < checkDist && CheckCollisionInTile(mode, new Vec2i(targetTilePos.X + 1, targetTilePos.Y - 1), targetPos, moveDir)) return true;
            if (relY > 1.0f - checkDist && CheckCollisionInTile(mode, new Vec2i(targetTilePos.X + 1, targetTilePos.Y + 1), targetPos, moveDir)) return true;
        }

        // Bottom
        if (relY < checkDist)
        {
            if (CheckCollisionInTile(mode, new Vec2i(targetTilePos.X, targetTilePos.Y - 1), targetPos, moveDir)) return true;
        }
        // Top
        else if (relY > 1.0f - checkDist)
        {
            if (CheckCollisionInTile(mode, new Vec2i(targetTilePos.X, targetTilePos.Y + 1), targetPos, moveDir)) return true;
        }
        
        return false;
    }

    private bool CheckCollisionInTile(FactoryGame mode, Vec2i tilePos, Vec2 myPos, Vec2 moveDir)
    {
        if (!mode.World.InBounds(tilePos)) return false;
        
        var items = mode.World[tilePos].Items;
        float collisionDist = Size * 0.95f; // Increased from 0.8f to reduce overlap
        
        // Iterate backwards to allow removal of dead items
        for (int i = items.Count - 1; i >= 0; i--)
        {
            var item = items[i];
            if (item == null || item.IsDestroyed)
            {
                items.RemoveAt(i);
                continue;
            }

            if (item == this) continue;
            
            float distSq = (item.Position - myPos).LengthSquared();
            if (distSq < collisionDist * collisionDist)
            {
                // Collision detected!
                // Check if we should ignore it based on direction (zipper merge)
                if (moveDir.LengthSquared() > 0.001f)
                {
                    var toItem = item.Position - Position; // Vector from ME to ITEM
                    float dot = Vec2.Dot(toItem.Normalized(), moveDir);
                    
                    // If item is "in front" (dot > 0.5, ~60 deg cone), it blocks us.
                    // If item is to the side or behind, we ignore it to allow merging.
                    // BUT: If we are literally overlapping (dist very small), we must block to prevent stacking.
                    if (distSq < (Size * 0.5f) * (Size * 0.5f)) return true; // Hard block if overlapping
                    
                    if (dot > 0.5f) return true;
                }
                else
                {
                    return true;
                }
            }
        }
        return false;
    }

    public override void Destroy()
    {
        if (IsDestroyed) return;
        IsDestroyed = true;
        Entity.Destroy();
    }
}
