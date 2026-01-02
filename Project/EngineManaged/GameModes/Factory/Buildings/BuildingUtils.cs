using EngineManaged.Numeric;
using SlimeCore.GameModes.Factory.Actors;
using SlimeCore.GameModes.Factory.Items;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.Source.World.Grid;

namespace SlimeCore.GameModes.Factory.Buildings;

public static class BuildingUtils
{
    public static bool TryOutputToConveyor(FactoryGame game, BuildingInstance instance, string itemId)
    {
        var dirs = new[] { Direction.North, Direction.East, Direction.South, Direction.West };
        
        for (int i = 1; i <= 4; i++)
        {
            int idx = (instance.LastOutputIndex + i) % 4;
            var dir = dirs[idx];
            
            var (nx, ny) = GetNeighbor(instance.X, instance.Y, dir);
            var conveyorDir = game.ConveyorSystem.GetConveyorDirection(nx, ny);
            
            if (conveyorDir.HasValue)
            {
                if (conveyorDir.Value == dir.Opposite())
                {
                    continue;
                }

                if (IsOutputBlocked(game.World, nx, ny, dir))
                {
                    continue;
                }

                var spawnPos = new Vec2(instance.X + 0.5f, instance.Y + 0.5f);

                if (IsSpawnBlocked(game.World, instance.X, instance.Y, spawnPos))
                {
                    continue;
                }

                var itemDef = ItemRegistry.Get(itemId);
                if (itemDef != null)
                {
                    float speed = 4.0f;
                    float dx = 0, dy = 0;
                    switch(dir) {
                        case Direction.North: dy = 1; break;
                        case Direction.East: dx = 1; break;
                        case Direction.South: dy = -1; break;
                        case Direction.West: dx = -1; break;
                    }
                    var velocity = new Vec2(dx, dy) * speed;

                    var dropped = new DroppedItem(spawnPos, itemDef, 1);
                    dropped.Velocity = velocity;
                    dropped.EjectionTimer = 0.5f;
                    game.ActorManager?.Register(dropped);
                    
                    instance.LastOutputIndex = idx;
                    return true;
                }
            }
        }
        return false;
    }

    private static bool IsSpawnBlocked(FactoryWorld world, int bx, int by, Vec2 spawnPos)
    {
        if (!world.InBounds(bx, by)) return false;
        var tile = world[bx, by];
        var items = tile.Items;
        float checkRadius = 0.4f;

        for(int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null || item.IsDestroyed) continue;
            
            float distSq = (item.Position - spawnPos).LengthSquared();
            if (distSq < checkRadius * checkRadius)
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsOutputBlocked(FactoryWorld world, int tx, int ty, Direction outputDir)
    {
        if (!world.InBounds(tx, ty)) return true;
        
        var tile = world[tx, ty];
        var items = tile.Items;
        
        float ex = tx + 0.5f;
        float ey = ty + 0.5f;
        
        switch (outputDir)
        {
            case Direction.North: ey = ty + 0.1f; break;
            case Direction.South: ey = ty + 0.9f; break;
            case Direction.East: ex = tx + 0.1f; break;
            case Direction.West: ex = tx + 0.9f; break;
        }
        
        var entryPos = new Vec2(ex, ey);
        float checkRadius = 0.4f;
        
        for(int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null || item.IsDestroyed) continue;
            
            float distSq = (item.Position - entryPos).LengthSquared();
            if (distSq < checkRadius * checkRadius)
            {
                return true;
            }
        }
        
        return false;
    }

    private static (int x, int y) GetNeighbor(int x, int y, Direction dir)
    {
        return dir switch
        {
            Direction.North => (x, y + 1),
            Direction.East => (x + 1, y),
            Direction.South => (x, y - 1),
            Direction.West => (x - 1, y),
            _ => (x, y)
        };
    }
}
