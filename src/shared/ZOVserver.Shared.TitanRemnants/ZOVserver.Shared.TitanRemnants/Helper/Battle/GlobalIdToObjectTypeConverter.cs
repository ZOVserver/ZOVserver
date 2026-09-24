using ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Game;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.Helper.Battle;

public static class GlobalIdToObjectTypeConverter
{
    public static GameObjectTypeHelperTable? GetObjectType(int globalId)
    {
        return GlobalId.GetClassId(globalId) switch
        {
            16 => GameObjectTypeHelperTable.Character,
            6 => GameObjectTypeHelperTable.Projectile,
            17 => GameObjectTypeHelperTable.AreaEffect,
            18 => GameObjectTypeHelperTable.Item,
            _ => null
        };
    }
}