using EngineManaged.Scene;
using SlimeCore.Source.World.Grid;
using System.ComponentModel.DataAnnotations.Schema;

namespace SlimeCore.GameModes.Factory.World;

public class RenderTile
{
    public Entity TerrainEntity;
    public TransformComponent TerrainTransform;
    public SpriteComponent TerrainSprite;

    public Entity OreEntity;
    public TransformComponent OreTransform;
    public SpriteComponent OreSprite;

    public Entity StructureEntity;
    public TransformComponent StructureTransform;
    public SpriteComponent StructureSprite;

    public bool IsVisible;
}

public class FactoryWorld : GridSystem<FactoryTerrain, FactoryTileOptions, FactoryTile>
{
    [NotMapped]
    public RenderTile[][]? RenderTiles { get; set; }

    [NotMapped]
    public float Zoom { get; set; } = 1.0f;

    public FactoryWorld(int worldWidth, int worldHeight, FactoryTerrain init_type, float zoom) : base(worldWidth, worldHeight, init_type)
    {
        Zoom = zoom;
    }

    public void Initialize(int viewWidth, int viewHeight)
    {
        RenderTiles = new RenderTile[viewWidth][];
        for (var x = 0; x < viewWidth; x++)
        {
            RenderTiles[x] = new RenderTile[viewHeight];
            for (var y = 0; y < viewHeight; y++)
            {
                // Layer 0: Terrain
                var tEntity = SceneFactory.CreateQuad(0, 0, Zoom, Zoom, 0.2f, 0.8f, 0.2f, layer: 0);
                var tTrans = tEntity.GetComponent<TransformComponent>();
                tTrans.Anchor = (0.5f, 0.5f);
                tTrans.Layer = 0;
                var tSprite = tEntity.GetComponent<SpriteComponent>();

                // Layer 1: Ore
                var oEntity = SceneFactory.CreateQuad(0, 0, Zoom, Zoom, 1, 1, 1, layer: 1);
                var oTrans = oEntity.GetComponent<TransformComponent>();
                oTrans.Anchor = (0.5f, 0.5f);
                oTrans.Layer = 1;
                var oSprite = oEntity.GetComponent<SpriteComponent>();
                oSprite.IsVisible = false; // Hidden by default

                // Layer 2: Structure
                var sEntity = SceneFactory.CreateQuad(0, 0, Zoom, Zoom, 1, 1, 1, layer: 2);
                var sTrans = sEntity.GetComponent<TransformComponent>();
                sTrans.Anchor = (0.5f, 0.5f);
                sTrans.Layer = 2;
                var sSprite = sEntity.GetComponent<SpriteComponent>();
                sSprite.IsVisible = false; // Hidden by default

                RenderTiles[x][y] = new RenderTile
                {
                    TerrainEntity = tEntity,
                    TerrainTransform = tTrans,
                    TerrainSprite = tSprite,

                    OreEntity = oEntity,
                    OreTransform = oTrans,
                    OreSprite = oSprite,

                    StructureEntity = sEntity,
                    StructureTransform = sTrans,
                    StructureSprite = sSprite,

                    IsVisible = true
                };
            }
        }
    }

    public void Destroy()
    {
        if (RenderTiles == null) return;

        foreach (var tiles in RenderTiles)
        {
            foreach (var tile in tiles)
            {
                tile.TerrainEntity.Destroy();
                tile.OreEntity.Destroy();
                tile.StructureEntity.Destroy();
            }
        }

        RenderTiles = null;
    }
}
