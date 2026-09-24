using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class GetSeasonRewardsMessage : PiranhaMessage
{
    [Field(0)] public int UnkVInt { get; set; }

    public override int GetMessageType()
    {
        return 14277;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}