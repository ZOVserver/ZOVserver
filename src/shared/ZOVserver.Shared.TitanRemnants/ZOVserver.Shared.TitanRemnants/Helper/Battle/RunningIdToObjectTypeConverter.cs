using ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Game;

namespace ZOVserver.Shared.TitanRemnants.Helper.Battle;

public static class RunningIdToObjectTypeConverter
{
    public static bool TryGetObjectType(int id, out GameObjectTypeHelperTable type)
    {
        var index = id / 1_000_000 - 1;

        if (index is >= byte.MinValue and <= byte.MaxValue)
        {
            var byteValue = (byte)index;

            if (Enum.IsDefined(typeof(GameObjectTypeHelperTable), byteValue))
            {
                type = (GameObjectTypeHelperTable)byteValue;
                return true;
            }
        }

        type = default;
        return false;
    }
}