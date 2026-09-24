using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class CreateAllianceMessage : PiranhaMessage
{
    [Field(0)] public string Name { get; set; } = string.Empty;

    [Field(1)] public string Description { get; set; } = string.Empty;

    [Field(2)] public int BadgeGlobalId { get; set; }

    [Field(3)] public int RegionGlobalId { get; set; }

    [Field(4, IsVarInt = true)] public int AllianceType { get; set; }

    [Field(5, IsVarInt = true)] public int RequiredTrophies { get; set; }

    public override int GetMessageType()
    {
        return 14301;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}