using System;

namespace SlimeCore.GameModes.Factory;

public static class FactoryResources
{
    public static IntPtr TexGrass;
    public static IntPtr TexConcrete;
    public static IntPtr TexWater;
    public static IntPtr TexSand;
    public static IntPtr TexStone;

    public static IntPtr TexOreIron;
    public static IntPtr TexOreCopper;
    public static IntPtr TexOreCoal;
    public static IntPtr TexOreGold;

    public static IntPtr TexConveyor;

    public static IntPtr TexSheep;

    public static void Load()
    {
        // Assuming textures are in Textures/Factory/ relative to the executable or content root
        TexGrass = NativeMethods.Resources_LoadTexture("grass", "Textures/Factory/grass.png");
        TexConcrete = NativeMethods.Resources_LoadTexture("concrete", "Textures/Factory/concrete.png");
        TexWater = NativeMethods.Resources_LoadTexture("water", "Textures/Factory/water.png");
        TexSand = NativeMethods.Resources_LoadTexture("sand", "Textures/Factory/sand.png");
        TexStone = NativeMethods.Resources_LoadTexture("stone", "Textures/Factory/stone.png");

        TexOreIron = NativeMethods.Resources_LoadTexture("ore_iron", "Textures/Factory/ore_iron.png");
        TexOreCopper = NativeMethods.Resources_LoadTexture("ore_copper", "Textures/Factory/ore_copper.png");
        TexOreCoal = NativeMethods.Resources_LoadTexture("ore_coal", "Textures/Factory/ore_coal.png");
        TexOreGold = NativeMethods.Resources_LoadTexture("ore_gold", "Textures/Factory/ore_gold.png");

        TexConveyor = NativeMethods.Resources_LoadTexture("conveyor", "Textures/Factory/conveyor.png");

        TexConveyor = NativeMethods.Resources_LoadTexture("conveyor", "Textures/Factory/Fauna/sheep.png");
    }

    public static IntPtr GetTerrainTexture(SlimeCore.GameModes.Factory.World.FactoryTerrain type)
    {
        return type switch
        {
            SlimeCore.GameModes.Factory.World.FactoryTerrain.Grass => TexGrass,
            SlimeCore.GameModes.Factory.World.FactoryTerrain.Concrete => TexConcrete,
            SlimeCore.GameModes.Factory.World.FactoryTerrain.Water => TexWater,
            SlimeCore.GameModes.Factory.World.FactoryTerrain.Sand => TexSand,
            SlimeCore.GameModes.Factory.World.FactoryTerrain.Stone => TexStone,
            _ => IntPtr.Zero
        };
    }

    public static IntPtr GetOreTexture(SlimeCore.GameModes.Factory.World.FactoryOre ore)
    {
        return ore switch
        {
            SlimeCore.GameModes.Factory.World.FactoryOre.Iron => TexOreIron,
            SlimeCore.GameModes.Factory.World.FactoryOre.Copper => TexOreCopper,
            SlimeCore.GameModes.Factory.World.FactoryOre.Coal => TexOreCoal,
            SlimeCore.GameModes.Factory.World.FactoryOre.Gold => TexOreGold,
            _ => IntPtr.Zero
        };
    }

    public static IntPtr GetStructureTexture(SlimeCore.GameModes.Factory.World.FactoryStructure structure)
    {
        return structure switch
        {
            SlimeCore.GameModes.Factory.World.FactoryStructure.ConveyorBelt => TexConveyor,
            _ => IntPtr.Zero
        };
    }

    public static IntPtr GetActorTexture(string actor)
    {
        return actor switch
        {
            "Sheep" => TexSheep,
            _ => IntPtr.Zero
        };
    }
}
