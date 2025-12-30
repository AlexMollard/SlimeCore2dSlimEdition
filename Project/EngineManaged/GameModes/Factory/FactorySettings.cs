using System;

namespace SlimeCore.GameModes.Factory;

public class FactorySettings
{
    public float InitialZoom { get; set; } = 1.0f;
    public int WorldWidth { get; set; } = 500;
    public int WorldHeight { get; set; } = 500;

    public int ActorBudget { get; set; } = 100;

    public int WorldBudget { get; set; } = 100;

    public int Seed { get; set; } = Random.Shared.Next();
}
