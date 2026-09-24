using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Commands.FromClient;

[LaserSerializable]
public partial class LogicClaimRankUpRewardCommand : LogicCommand
{
    [Field(0, IsVarInt = true, BaseMethodBefore = true)]
    public int Type { get; set; }

    [Field(1, AsDataRef = true)] public int DataRefX { get; set; }

    [Field(2, IsVarInt = true)] public int Y { get; set; }

    public override int GetCommandType()
    {
        return 517;
    }
}