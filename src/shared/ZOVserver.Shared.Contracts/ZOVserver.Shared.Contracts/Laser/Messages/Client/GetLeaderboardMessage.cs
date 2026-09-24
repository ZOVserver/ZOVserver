using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class GetLeaderboardMessage : PiranhaMessage
{
    [Field(0)] public bool IsRegional { get; set; }
    [Field(2, IsVarInt = true)] public int LeaderboardType { get; set; }
    [Field(3, AsDataRef = true)] public int BrawlerGlobalId { get; set; }
    [Field(4, IsVarInt = true)] public int UnkVInt { get; set; }

    public override int GetMessageType()
    {
        return 14403;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}