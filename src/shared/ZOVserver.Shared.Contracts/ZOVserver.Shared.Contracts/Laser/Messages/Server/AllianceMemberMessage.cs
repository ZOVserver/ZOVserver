using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class AllianceMemberMessage : PiranhaMessage
{
    [Field(0)] public long AllianceId { get; set; }
    [Field(1)] public AllianceMemberEntry? MemberEntry { get; set; }

    public override int GetMessageType()
    {
        return 24308;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}