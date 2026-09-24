using MessagePack;
using Orleans;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Friend;

[LaserSerializable]
[GenerateSerializer]
[MessagePackObject]
[Alias("Friend.FriendAllianceSegment")]
public partial class FriendAllianceSegment : LaserContract
{
    [Field(0)] [Id(0)] [Key(0)] public long AllianceId { get; set; }
    [Field(1)] [Id(1)] [Key(1)] public int Unk1 { get; set; }
    [Field(2)] [Id(2)] [Key(2)] public string AllianceName { get; set; } = string.Empty;
    [Field(3)] [Id(3)] [Key(3)] public int Unk2 { get; set; }
    [Field(4)] [Id(4)] [Key(4)] public int Unk3 { get; set; }
}