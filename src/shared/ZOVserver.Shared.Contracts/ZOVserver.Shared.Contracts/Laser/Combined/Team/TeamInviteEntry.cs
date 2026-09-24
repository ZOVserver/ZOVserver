using Orleans;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Team;

[LaserSerializable]
[GenerateSerializer]
[Alias("Team.TeamInviteEntry")]
public partial class TeamInviteEntry : LaserContract
{
    [Field(0)] [Id(0)] public long InviterId { get; set; }

    [Field(1)] [Id(1)] public long TargetId { get; set; }
    [Field(2)] [Id(2)] public string TargetName { get; set; } = string.Empty;

    [Field(3, IsVarInt = true)] [Id(3)] public int Status { get; set; }
    [Field(4, IsVarInt = true)] [Id(4)] public int TeamIndex { get; set; }

    [Id(5)] public long TeamId { get; set; }

    [Id(6)] public string InviterName { get; set; } = string.Empty;
}