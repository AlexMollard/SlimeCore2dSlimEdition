using System;

namespace SlimeCore.GameModes.Factory;

public static class FactoryResources
{

    public static IntPtr TexGrass;
    public static IntPtr TexConcrete;
    public static IntPtr TexWater;
    public static IntPtr TexSand;
    public static IntPtr TexStone;
    public static IntPtr TexDirt;

    public static IntPtr TexOreIron;
    public static IntPtr TexOreCopper;
    public static IntPtr TexOreCoal;
    public static IntPtr TexOreGold;

    public static IntPtr TexConveyor;
    public static IntPtr TexMinerT1;
    public static IntPtr TexMinerT2;
    public static IntPtr TexMinerT3;
    public static IntPtr TexStorageT1;
    public static IntPtr TexStorageT2;
    public static IntPtr TexStorageT3;
    public static IntPtr TexFarmPlot;
    public static IntPtr TexWall;

    public static IntPtr TexItemIronOre;
    public static IntPtr TexItemCopperOre;
    public static IntPtr TexItemCoal;
    public static IntPtr TexItemGoldOre;
    public static IntPtr TexItemStone;
    public static IntPtr TexItemVegetable;
    //Fauna Textures
    public static IntPtr TexSheep { get; set; }
    public static IntPtr TexWolf { get; set; }
    //Flora Textures
    public static IntPtr TexTree01 { get; set; }


    public static IntPtr TexDebug;

    private static System.Collections.Generic.Dictionary<string, IntPtr> _textureCache = new();

    public static IntPtr GetOrCreateTexture(string name, string path)
    {
        if (_textureCache.TryGetValue(name, out var ptr))
        {
            return ptr;
        }
        
        ptr = NativeMethods.Resources_LoadTexture(name, path);
        _textureCache[name] = ptr;
        return ptr;
    }

	public static void Load()
    {
		TexDebug = NativeMethods.Resources_LoadTexture("debug", "Textures/debug.png");


		// Assuming textures are in Textures/Factory/ relative to the executable or content root
		TexGrass = NativeMethods.Resources_LoadTexture("grass", "Textures/Factory/grass.png");
        TexConcrete = NativeMethods.Resources_LoadTexture("concrete", "Textures/Factory/concrete.png");
        TexWater = NativeMethods.Resources_LoadTexture("water", "Textures/Factory/water.png");
        TexSand = NativeMethods.Resources_LoadTexture("sand", "Textures/Factory/sand.png");
        TexStone = NativeMethods.Resources_LoadTexture("stone", "Textures/Factory/stone.png");
		TexDirt = NativeMethods.Resources_LoadTexture("dirt", "Textures/Factory/dirt.png");

        TexOreIron = NativeMethods.Resources_LoadTexture("ore_iron", "Textures/Factory/ore_iron.png");
        TexOreCopper = NativeMethods.Resources_LoadTexture("ore_copper", "Textures/Factory/ore_copper.png");
        TexOreCoal = NativeMethods.Resources_LoadTexture("ore_coal", "Textures/Factory/ore_coal.png");
        TexOreGold = NativeMethods.Resources_LoadTexture("ore_gold", "Textures/Factory/ore_gold.png");

        TexConveyor = NativeMethods.Resources_LoadTexture("conveyor", "Textures/Factory/conveyor.png");
        
        TexMinerT1 = NativeMethods.Resources_LoadTexture("miner_t1", "Textures/Factory/miner_t1.png");
        TexMinerT2 = NativeMethods.Resources_LoadTexture("miner_t2", "Textures/Factory/miner_t2.png");
        TexMinerT3 = NativeMethods.Resources_LoadTexture("miner_t3", "Textures/Factory/miner_t3.png");
        
        TexStorageT1 = NativeMethods.Resources_LoadTexture("storage_t1", "Textures/Factory/storage_t1.png");
        TexStorageT2 = NativeMethods.Resources_LoadTexture("storage_t2", "Textures/Factory/storage_t2.png");
        TexStorageT3 = NativeMethods.Resources_LoadTexture("storage_t3", "Textures/Factory/storage_t3.png");

        TexFarmPlot = NativeMethods.Resources_LoadTexture("farm_plot", "Textures/Factory/Structures/farmplot.png");
        TexWall = NativeMethods.Resources_LoadTexture("wall", "Textures/Factory/Structures/wall.png");

        TexItemIronOre = NativeMethods.Resources_LoadTexture("item_iron_ore", "Textures/Factory/Items/iron_ore.png");
        TexItemCopperOre = NativeMethods.Resources_LoadTexture("item_copper_ore", "Textures/Factory/Items/copper_ore.png");
        TexItemCoal = NativeMethods.Resources_LoadTexture("item_coal", "Textures/Factory/Items/coal.png");
        TexItemGoldOre = NativeMethods.Resources_LoadTexture("item_gold_ore", "Textures/Factory/Items/gold_ore.png");
        TexItemStone = NativeMethods.Resources_LoadTexture("item_stone", "Textures/Factory/Items/stone.png");
        TexItemVegetable = NativeMethods.Resources_LoadTexture("item_vegetable", "Textures/Factory/Items/vegetable.png");

        TexSheep = NativeMethods.Resources_LoadTexture("sheep", "Textures/Factory/Fauna/sheep.png");
        TexWolf = NativeMethods.Resources_LoadTexture("wolf", "Textures/Factory/Fauna/wolf.png");


        TexTree01 = NativeMethods.Resources_LoadTexture("tree01", "Textures/Factory/Flora/tree01.png");
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
            SlimeCore.GameModes.Factory.World.FactoryTerrain.Dirt => TexDirt,
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

    public static IntPtr GetStructureTexture(SlimeCore.GameModes.Factory.World.FactoryStructure structure, int tier = 1)
    {
        return structure switch
        {
            SlimeCore.GameModes.Factory.World.FactoryStructure.ConveyorBelt => TexConveyor,
            SlimeCore.GameModes.Factory.World.FactoryStructure.Miner => tier switch 
            {
                2 => TexMinerT2,
                3 => TexMinerT3,
                _ => TexMinerT1
            },
            SlimeCore.GameModes.Factory.World.FactoryStructure.Storage => tier switch
            {
                2 => TexStorageT2,
                3 => TexStorageT3,
                _ => TexStorageT1
            },
            SlimeCore.GameModes.Factory.World.FactoryStructure.FarmPlot => TexFarmPlot,
            SlimeCore.GameModes.Factory.World.FactoryStructure.Wall => TexWall,
            _ => IntPtr.Zero
        };
    }

    public static IntPtr GetItemTexture(SlimeCore.GameModes.Factory.World.FactoryItemType item)
    {
        return item switch
        {
            SlimeCore.GameModes.Factory.World.FactoryItemType.IronOre => TexItemIronOre,
            SlimeCore.GameModes.Factory.World.FactoryItemType.CopperOre => TexItemCopperOre,
            SlimeCore.GameModes.Factory.World.FactoryItemType.Coal => TexItemCoal,
            SlimeCore.GameModes.Factory.World.FactoryItemType.GoldOre => TexItemGoldOre,
            SlimeCore.GameModes.Factory.World.FactoryItemType.Stone => TexItemStone,
            SlimeCore.GameModes.Factory.World.FactoryItemType.Vegetable => TexItemVegetable,
            _ => IntPtr.Zero
        };
    }

    public static IntPtr GetActorTexture(string actor)
    {
        return actor switch
        {
            "Sheep" => TexSheep,
            "Wolf" => TexWolf,
            _ => IntPtr.Zero
        };
    }
}
