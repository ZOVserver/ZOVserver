using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class AllianceDataMessage : PiranhaMessage
{
    [Field(0)] public bool IsMyAlliance { get; set; }
    [Field(1)] public AllianceFullEntry? AllianceFullEntry { get; set; }

    public override int GetMessageType()
    {
        return 24301;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}