using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Commands.FromClient;

[LaserSerializable]
public partial class LogicClaimDailyRewardCommand : LogicCommand
{
    [Field(0, IsVarInt = true, BaseMethodBefore = true)]
    public int EventSlot { get; set; }

    [Field(1)] public int UnkVInt { get; set; }

    public override int GetCommandType()
    {
        return 503;
    }
}