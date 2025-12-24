using EngineManaged.Numeric;
using EngineManaged.Scene;
using GameModes.Dude;
using SlimeCore.Core.World;
using SlimeCore.Source.World.Actors;
using SlimeCore.Source.World.Grid;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.GameModes.Snake.Actors;

public record NPC_SnakeHunter : Actor<Terrain>
{
    public enum HunterType
    { 
        Normal, 
        Chonker 
    }
    public Entity HunterEntity { get; set; }

    public HunterType Type { get; set; }

    public void SpawnHunter(SnakeGame game)
    {
        HunterType type = game.Rng.NextDouble() < 0.10 ? HunterType.Chonker : HunterType.Normal;
        Vec2i pos;
        if (game.Rng.Next(2) == 0) { pos.X = game.Rng.Next(2) == 0 ? -16 : 16; pos.Y = (game.Rng.Next() * 20) - 10; }
        else { pos.X = (game.Rng.Next() * 28) - 14; pos.Y = game.Rng.Next(2) == 0 ? -11 : 11; }
        float size = type == HunterType.Chonker ? 2.2f : 0.8f;
        float r = type == HunterType.Chonker ? 0.6f : 1.0f;
        float b = type == HunterType.Chonker ? 0.8f : 0.2f;
        var ent = game.CreateSpriteEntity(pos.X, pos.Y, size, size, r, 0f, b, 5, game.TexEnemy);

        // Add Physics (Kinematic so we can control position manually but still collide with player)
        ent.AddComponent<RigidBodyComponent>();
        ent.AddComponent<BoxColliderComponent>();
        var rb = ent.GetComponent<RigidBodyComponent>();
        rb.IsKinematic = true;
        var bc = ent.GetComponent<BoxColliderComponent>();
        bc.Size = (size * 0.8f, size * 0.8f); // Slightly smaller hitbox

        var haterTransform = ent.GetComponent<TransformComponent>();
        haterTransform.Anchor = (0.5f, 0.5f);
        game.Hunters.Add(new NPC_SnakeHunter { HunterEntity = ent, Position = pos, Type = type });
    }
}
