using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Alliance;

[LaserSerializable]
public partial class StatusChangeEntry : LaserContract
{
    [Field(0)] public long AccountId { get; set; }
    [Field(1, IsVarInt = true)] public int Status { get; set; }
}