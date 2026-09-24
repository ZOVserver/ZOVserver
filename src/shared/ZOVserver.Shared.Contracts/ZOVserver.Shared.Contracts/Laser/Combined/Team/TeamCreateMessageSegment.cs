using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Team;

[LaserSerializable]
public partial class TeamCreateMessageSegment : LaserContract
{
    [Field(0)] public long UnknownId { get; set; }
    [Field(1, IsVarInt = true)] public int UnknownVInt { get; set; }
    [Field(2, AsDataRef = true)] public int UnknownDataRef { get; set; }
}