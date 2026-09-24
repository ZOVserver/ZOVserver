using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Alliance;

[LaserSerializable]
public partial class AllianceMemberEntry : LaserContract
{
    [Field(0)] public long AccountId { get; set; }
    [Field(1, IsVarInt = true)] public int Role { get; set; }
    [Field(2, IsVarInt = true)] public int Trophies { get; set; }
    [Field(3, IsVarInt = true)] public int Status { get; set; }

    [Field(4, IsVarInt = true, LastOnlineTime = true)]
    public DateTime LastOnlineTime { get; set; }

    [Field(5)] public bool InvitesBlocked { get; set; }
    [Field(6)] public PlayerDisplayData? DisplayData { get; set; }
}