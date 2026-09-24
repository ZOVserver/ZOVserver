using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class AllianceListMessage : PiranhaMessage
{
    [Field(0)] public string SearchString { get; set; } = string.Empty;
    [Field(1)] public AllianceHeaderEntry[] Alliances { get; set; } = [];

    public override int GetMessageType()
    {
        return 24310;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}