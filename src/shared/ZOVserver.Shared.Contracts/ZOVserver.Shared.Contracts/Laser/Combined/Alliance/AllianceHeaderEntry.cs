using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Alliance;

[LaserSerializable]
public partial class AllianceHeaderEntry : LaserContract
{
    [Field(0)] public long AllianceId { get; set; }
    [Field(1)] public string AllianceName { get; set; } = string.Empty;

    [Field(2)] public int BadgeGlobalId { get; set; }
    [Field(3, IsVarInt = true)] public int AllianceType { get; set; }
    [Field(4, IsVarInt = true)] public int MembersCount { get; set; }
    [Field(5, IsVarInt = true)] public int NowTrophies { get; set; }
    [Field(6, IsVarInt = true)] public int RequiredTrophies { get; set; }
    [Field(7)] public int PreferredLanguageGlobalId { get; set; }
    [Field(8)] public string Region { get; set; } = "RU";
    [Field(9, IsVarInt = true)] public int Unk2 { get; set; }
}