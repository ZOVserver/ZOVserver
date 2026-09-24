using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Team;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class TeamPremadeChatMessage : PiranhaMessage
{
    [Field(0, AsDataRef = true)] public int MessageGlobalId { get; set; }
    [Field(1, PresenceBool = true)] public TeamPremadeChatTargetPlayer? TargetPlayer { get; set; }
    [Field(2, IsVarInt = true)] public int EventSlot { get; set; }
    [Field(3, IsVarInt = true)] public int DataId { get; set; }
    [Field(4, IsVarInt = true)] public int LocationId { get; set; }

    public override int GetMessageType()
    {
        return 14369;
    }

    public override int GetServiceNodeType()
    {
        return 23;
    }
}