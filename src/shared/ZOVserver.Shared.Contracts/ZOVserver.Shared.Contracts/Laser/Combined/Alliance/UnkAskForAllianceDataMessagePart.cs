using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Alliance;

[LaserSerializable]
public partial class UnkAskForAllianceDataMessagePart : LaserContract
{
    [Field(0)] public long UnknownId { get; set; }
}