using MessagePack;
using Orleans;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Friend;

[LaserSerializable]
[GenerateSerializer]
[MessagePackObject]
[Alias("Friend.FriendEntry")]
public partial class FriendEntry : LaserContract
{
    [Field(0)] [Id(0)] [Key(0)] public long AccountId { get; set; }

    [Field(1)] [Id(1)] [Key(1)] public string Unk1 { get; set; } = string.Empty;
    [Field(2)] [Id(2)] [Key(2)] public string Unk2 { get; set; } = string.Empty;
    [Field(3)] [Id(3)] [Key(3)] public string Unk3 { get; set; } = string.Empty;
    [Field(4)] [Id(4)] [Key(4)] public string Unk4 { get; set; } = string.Empty;
    [Field(5)] [Id(5)] [Key(5)] public string Unk5 { get; set; } = string.Empty;
    [Field(6)] [Id(6)] [Key(6)] public string Unk6 { get; set; } = string.Empty;

    [Field(7)] [Id(7)] [Key(7)] public int Trophies { get; set; }

    [Field(8)] [Id(8)] [Key(8)] public int FriendState { get; set; }
    [Field(9)] [Id(9)] [Key(9)] public int FriendReason { get; set; }

    [Field(10)] [Id(10)] [Key(10)] public int Unk7 { get; set; }
    [Field(11)] [Id(11)] [Key(11)] public int Unk8 { get; set; }

    [Field(12, PresenceBool = true)]
    [Id(12)]
    [Key(12)]
    public FriendAllianceSegment? Alliance { get; set; }

    [Field(13)] [Id(13)] [Key(13)] public string Unk9 { get; set; } = string.Empty;

    [Field(14, LastOnlineTime = true)]
    [Id(14)]
    [Key(14)]
    public DateTime LastOnlineTime { get; set; }

    [Field(15, PresenceBool = true)]
    [Id(15)]
    [Key(15)]
    public PlayerDisplayData? DisplayData { get; set; }
}