using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class AllianceTeamsMessage : PiranhaMessage
{
    [Field(0)] public bool ClearTeams { get; set; }
    [Field(1)] public List<AllianceTeamEntry> AllianceTeamEntries { get; set; } = [];

    public override int GetMessageType()
    {
        return 24364;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}