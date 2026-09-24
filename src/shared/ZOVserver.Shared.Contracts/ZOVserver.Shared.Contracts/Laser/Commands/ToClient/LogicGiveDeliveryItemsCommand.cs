using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Delivery;
using ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;

namespace ZOVserver.Shared.Contracts.Laser.Commands.ToClient;

[LaserSerializable]
public partial class LogicGiveDeliveryItemsCommand : LogicServerCommand
{
    [Field(0)] public int Unk1VInt { get; set; }

    [Field(1)] public List<DeliveryUnit>? DeliveryUnits { get; set; }

    [Field(2, PresenceBool = true)] public ForcedDrops? ForcedDrops { get; set; }

    [Field(3, IsVarInt = true)] public int RewardTrackType { get; set; }

    [Field(4, IsVarInt = true, AddNumber = 1)]
    public int RewardForRankType { get; set; } = -1;

    [Field(5, BaseMethodAfter = true)] public int Unk2VInt { get; set; }

    public override int GetCommandType()
    {
        return 203;
    }
}