using EngineManaged.Numeric;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.Source.Common;
using SlimeCore.Source.Core;
using System;

namespace SlimeCore.GameModes.Factory;

public static class FactoryPhysics
{
    public static void ApplyConveyorMovement(FactoryGame game, ref Vec2 position, float dt, float size, bool ignoreCollision = false, Func<Vec2, bool>? extraCollisionCheck = null)
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
                float conveyorSpeed = baseSpeed * Math.Max(1, tile.Tier); // Ensure at least tier 1 speed
                
                var velocity = GetConveyorVelocity(tile, position.X - gx, position.Y - gy);
                
                var move = velocity * conveyorSpeed * dt;
                var targetPos = position + move;

                // Apply conveyor movement with collision check
                if ((ignoreCollision || !CheckCollision(game, targetPos, size)) && (extraCollisionCheck == null || !extraCollisionCheck(targetPos)))
                {
                    position += move;
                }
                else
                {
                    // Logger.Trace($"Collision blocked conveyor move at {gx},{gy}. Move: {move}");
                }
            }
        }
    }

    private static Vec2 GetConveyorVelocity(FactoryTile tile, float lx, float ly)
    {
        var outDir = tile.Direction;
        var outVec = GetDirectionVector(outDir);
        
        // 1. Determine if we are Downstream (past the center)
        // Project position onto output axis relative to center (0.5, 0.5)
        float proj = (lx - 0.5f) * outVec.X + (ly - 0.5f) * outVec.Y;
        
        if (proj >= 0)
        {
            // Downstream: Move straight to output, center on axis
            // Calculate distance from center line
            float dist = 0;
            if (outDir == Direction.North || outDir == Direction.South) dist = 0.5f - lx;
            else dist = 0.5f - ly;
            
            // Apply centering force
            var centering = new Vec2(0, 0);
            if (outDir == Direction.North || outDir == Direction.South) centering = new Vec2(dist * 5.0f, 0);
            else centering = new Vec2(0, dist * 5.0f);
            
            return outVec + centering;
        }
        
        // 2. Upstream: Determine which Input Sector we are in
        var inputSector = GetInputSector(lx, ly);
        
        if (inputSector.HasValue)
        {
            // Check if this sector has a valid input connection
            int mask = 1 << (int)inputSector.Value;
            if ((tile.ConveyorBitmask & mask) != 0)
            {
                // Valid Input!
                var inDir = inputSector.Value;
                
                // Is it a straight path?
                if (inDir == outDir.Opposite())
                {
                    // Straight
                    // Calculate distance from center line
                    float dist = 0;
                    if (outDir == Direction.North || outDir == Direction.South) dist = 0.5f - lx;
                    else dist = 0.5f - ly;
                    
                    var centering = new Vec2(0, 0);
                    if (outDir == Direction.North || outDir == Direction.South) centering = new Vec2(dist * 5.0f, 0);
                    else centering = new Vec2(0, dist * 5.0f);
                    
                    return outVec + centering;
                }
                else
                {
                    // Turn!
                    return GetTurnVelocity(outDir, inDir, lx, ly);
                }
            }
        }
        
        // 3. Fallback: Not in a valid input sector, or no input there.
        // Just push towards center (0.5, 0.5) then align with output
        float dx = 0.5f - lx;
        float dy = 0.5f - ly;
        var toCenter = new Vec2(dx, dy);
        if (toCenter.LengthSquared() > 0.001f)
            toCenter = toCenter.Normalized();
            
        // Blend with output vector
        return (toCenter + outVec).Normalized();
    }

    private static Direction? GetInputSector(float lx, float ly)
    {
        // Divide tile into 4 quadrants based on diagonals
        // y > x and y > 1-x => North
        // y < x and y < 1-x => South
        // y > x and y < 1-x => West (Wait: x < 0.5)
        // Let's use simple distance to edge
        
        float dN = 1.0f - ly;
        float dS = ly;
        float dE = 1.0f - lx;
        float dW = lx;
        
        float min = Math.Min(Math.Min(dN, dS), Math.Min(dE, dW));
        
        if (min == dN) return Direction.North;
        if (min == dS) return Direction.South;
        if (min == dE) return Direction.East;
        if (min == dW) return Direction.West;
        
        return null;
    }

    private static Vec2 GetTurnVelocity(Direction outDir, Direction inDir, float lx, float ly)
    {
        // 1. Determine Pivot Point (Shared Corner)
        float px = 0, py = 0;
        
        if ((outDir == Direction.North && inDir == Direction.East) || (outDir == Direction.East && inDir == Direction.North)) { px = 1; py = 1; }
        else if ((outDir == Direction.North && inDir == Direction.West) || (outDir == Direction.West && inDir == Direction.North)) { px = 0; py = 1; }
        else if ((outDir == Direction.South && inDir == Direction.East) || (outDir == Direction.East && inDir == Direction.South)) { px = 1; py = 0; }
        else if ((outDir == Direction.South && inDir == Direction.West) || (outDir == Direction.West && inDir == Direction.South)) { px = 0; py = 0; }
        
        // 2. Determine Rotation Direction (CW or CCW)
        // Input Vector (pointing INTO tile)
        var inVec = GetDirectionVector(inDir) * -1; 
        var outVec = GetDirectionVector(outDir);
        
        // Cross Product (2D)
        float cross = inVec.X * outVec.Y - inVec.Y * outVec.X;
        bool ccw = cross > 0;
        
        // 3. Calculate Tangent
        float dx = lx - px;
        float dy = ly - py;
        float r = (float)Math.Sqrt(dx*dx + dy*dy);
        
        Vec2 tangent;
        if (ccw)
            tangent = new Vec2(-dy, dx); // CCW 90 deg
        else
            tangent = new Vec2(dy, -dx); // CW 90 deg
            
        if (r > 0.001f)
            tangent = tangent.Normalized();
            
        // 4. Centering Force (Push towards radius 0.5)
        var centering = new Vec2(0, 0);
        if (r > 0.001f)
        {
            var radial = new Vec2(dx, dy) / r;
            float diff = 0.5f - r;
            centering = radial * diff * 5.0f;
        }
        
        return tangent + centering;
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
}
