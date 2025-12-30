using EngineManaged.Numeric;
using SlimeCore.GameModes.Factory.World;
using System;

namespace SlimeCore.GameModes.Factory;

public static class FactoryPhysics
{
    public static void ApplyConveyorMovement(FactoryGame game, ref Vec2 position, float dt, float size)
    {
        // Check center point for conveyor influence
        int gx = (int)Math.Floor(position.X);
        int gy = (int)Math.Floor(position.Y);
        
        if (gx >= 0 && gx < game.World.Width() && gy >= 0 && gy < game.World.Height())
        {
            var tile = game.World[gx, gy];
            if (tile.Structure == FactoryStructure.ConveyorBelt)
            {
                float baseSpeed = 2.0f;
                float conveyorSpeed = baseSpeed * tile.Tier; 
                
                var flowDir = GetConveyorFlow(tile, position.X - gx, position.Y - gy);
                
                var move = flowDir * conveyorSpeed * dt;
                
                // Apply conveyor movement with collision check
                if (!CheckCollision(game, position + move, size))
                {
                    position += move;
                }
            }
        }
    }

    public static bool CheckCollision(FactoryGame game, Vec2 pos, float size)
    {
        // Check corners of the bounding box
        float halfSize = size / 2.0f * 0.8f; // Slightly smaller hitbox than visual size
        
        // Check 4 corners
        if (IsSolid(game, pos.X - halfSize, pos.Y - halfSize)) return true;
        if (IsSolid(game, pos.X + halfSize, pos.Y - halfSize)) return true;
        if (IsSolid(game, pos.X - halfSize, pos.Y + halfSize)) return true;
        if (IsSolid(game, pos.X + halfSize, pos.Y + halfSize)) return true;
        
        return false;
    }

    public static bool IsSolid(FactoryGame game, float x, float y)
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

    private static Vec2 GetConveyorFlow(FactoryTile tile, float lx, float ly)
    {
        var outDir = tile.Direction;
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
            var flow = GetDirectionVector(outDir);
            
            // Centering Force: Push towards 0.5 on the non-flow axis
            var centering = new Vec2(0, 0);
            if (outDir == Direction.North || outDir == Direction.South)
                centering = new Vec2(0.5f - lx, 0);
            else
                centering = new Vec2(0, 0.5f - ly);
                
            return flow + centering * 0.5f;
        }
        
        // Turn Logic
        return GetTurnVector(outDir, inDir.Value, lx, ly);
    }

    private static Vec2 GetTurnVector(Direction outDir, Direction inDir, float lx, float ly)
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
        float r = (float)Math.Sqrt(dx*dx + dy*dy);
        
        // Tangent
        Vec2 tangent;
        if (ccw)
            tangent = new Vec2(-dy, dx);
        else
            tangent = new Vec2(dy, -dx);
        
        if (r > 0.001f)
            tangent = tangent.Normalized();
            
        // Centering Force: Push towards radius 0.5
        var centering = new Vec2(0, 0);
        if (r > 0.001f)
        {
            var radial = new Vec2(dx, dy) / r;
            float diff = 0.5f - r;
            centering = radial * diff;
        }
        
        return tangent + centering * 0.5f;
    }

    private static Vec2 GetDirectionVector(Direction dir)
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
}
