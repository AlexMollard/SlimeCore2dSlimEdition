using SlimeCore.GameModes.Factory.World;

namespace SlimeCore.GameModes.Factory.Buildings;

public interface IBuildingBehavior
{
    void Update(BuildingInstance instance, float dt, FactoryGame game);
    void OnPlace(BuildingInstance instance, FactoryGame game);
    void OnRemove(BuildingInstance instance, FactoryGame game);
}
