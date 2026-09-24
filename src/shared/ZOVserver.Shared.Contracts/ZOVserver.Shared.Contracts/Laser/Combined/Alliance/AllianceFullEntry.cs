using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Alliance;

[LaserSerializable]
public partial class AllianceFullEntry : LaserContract
{
    [Field(0)] public AllianceHeaderEntry? AllianceHeaderEntry { get; set; }
    [Field(1)] public string Description { get; set; } = string.Empty;
    [Field(2)] public List<AllianceMemberEntry> Members { get; set; } = [];
}