using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Alliance;

[LaserSerializable]
public partial class MyAllianceObject : LaserContract
{
    [Field(0)] public int MyRoleDataRef { get; set; }
    [Field(1)] public AllianceHeaderEntry? AllianceHeaderEntry { get; set; }
}