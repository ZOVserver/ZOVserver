using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class JoinableAllianceListMessage : PiranhaMessage
{
    [Field(0)] public AllianceHeaderEntry[] Alliances { get; set; } = [];

    public override int GetMessageType()
    {
        return 24304;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}