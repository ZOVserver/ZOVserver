using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Team;

[LaserSerializable]
[MessagePackObject]
public partial class TeamPremadeChatTargetPlayer : LaserContract
{
    [Field(0)] [Key(0)] public long PlayerId { get; set; }
}