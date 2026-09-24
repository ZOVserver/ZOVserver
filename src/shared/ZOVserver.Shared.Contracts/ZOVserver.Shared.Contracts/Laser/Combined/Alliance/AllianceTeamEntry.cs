using MessagePack;
using Orleans;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Alliance;

[GenerateSerializer]
[LaserSerializable]
[MessagePackObject]
[Alias("Alliance.AllianceTeamEntry")]
public partial class AllianceTeamEntry : LaserContract
{
    [Field(0, IsVarInt = true)]
    [Id(0)]
    [Key(0)]
    public int RoomType { get; set; }

    [Field(1, IsVarInt = true)]
    [Id(1)]
    [Key(1)]
    public int MaxPlayers { get; set; }

    [Field(2)] [Id(2)] [Key(2)] public long TeamId { get; set; }

    [Field(3, IsVarInt = true)]
    [Id(3)]
    [Key(3)]
    public int UnkVInt3 { get; set; }

    [Field(4, IsVarInt = true)]
    [Id(4)]
    [Key(4)]
    public long OwnerAccountId { get; set; }

    [Field(5, IsVarInt = true)]
    [Id(5)]
    [Key(5)]
    public long UnkVLong2 { get; set; }

    [Field(6)] [Id(6)] [Key(6)] public bool UnkBool1 { get; set; }
    [Field(7)] [Id(7)] [Key(7)] public bool UnkBool2 { get; set; }
    [Field(8)] [Id(8)] [Key(8)] public bool UnkBool3 { get; set; }

    [Field(9, IsVarInt = true)]
    [Id(9)]
    [Key(9)]
    public long[] Players { get; set; } = [];
}