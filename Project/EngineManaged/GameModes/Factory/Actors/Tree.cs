using EngineManaged.Numeric;
using EngineManaged.Scene;
using SlimeCore.Source.World.Actors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace SlimeCore.GameModes.Factory.Actors;

public class Tree : Actor<FactoryActors, FactoryGame>
{
    public override FactoryActors Kind => FactoryActors.Plants;
    protected override float ActionInterval => 0.10f;

    public Entity Entity { get; private set; }

    public float Growth { get; set; } = 0.5f;

    public Tree(Vec2i TreePos, float growth)
    {
        Position = TreePos;
        Growth = growth;
        Entity = SceneFactory.CreateQuad(Position.X, Position.Y, Growth, Growth, 1.0f, 1.0f, 1.0f, layer: 20);
        var sprite = Entity.GetComponent<SpriteComponent>();
        sprite.TexturePtr = FactoryResources.TexTree01;
    }

    public override bool TakeAction(FactoryGame mode, float deltaTime)
    {
        if (Growth > 10f)
        {
            return false;
        }
        if (mode.Rng.Next() == 0)
        {
            Growth += deltaTime;
            var transform = Entity.GetComponent<TransformComponent>();
            transform.Scale = (Growth, Growth);
        }
        return true;
    }

    public override void Destroy()
    {
        Entity.Destroy();
    }

    public static void Populate(FactoryGame game, int amount)
    {
        var coords = new Vec2i(game.Rng!.Next(game.World.Width()), game.Rng!.Next(game.World.Width()));
        int counter = 0;
        for (int i = 0; i < amount; i++)
        {
            float growth = (float)(game.Rng.NextDouble() * 2.0);
            coords += new Vec2i(game.Rng.Next(1,2), game.Rng.Next(1,1));
            game.ActorManager?.Register(new Tree(coords, growth));
            counter += game.Rng.Next(10);
            if (counter % 2 == 0)
            {
                coords = new Vec2i(game.Rng!.Next(game.World.Width()), game.Rng!.Next(game.World.Width()));
            }
        }
    }
}
