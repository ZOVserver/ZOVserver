using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class PlayerProfileMessage : PiranhaMessage
{
    [Field(0)] public PlayerProfile? PlayerProfile { get; set; }
    [Field(1, PresenceBool = true)] public AllianceHeaderEntry? AllianceHeader { get; set; }
    [Field(2, AsDataRef = true)] public int RoleGlobalId { get; set; }

    public override int GetMessageType()
    {
        return 24113;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}