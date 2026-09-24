using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class ChangeAllianceSettingsOkMessage : PiranhaMessage
{
    [Field(0)] public AllianceFullEntry? Alliance { get; set; }

    public override int GetMessageType()
    {
        return 24313;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}