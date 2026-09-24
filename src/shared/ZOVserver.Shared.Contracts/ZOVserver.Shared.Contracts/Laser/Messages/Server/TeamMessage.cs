using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Team;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class TeamMessage : PiranhaMessage
{
    [Field(0)] public TeamEntry? TeamEntry { get; set; }

    public override int GetMessageType()
    {
        return 24124;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}