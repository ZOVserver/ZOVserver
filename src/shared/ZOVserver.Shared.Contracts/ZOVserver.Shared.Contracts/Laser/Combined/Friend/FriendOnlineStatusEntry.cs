using MessagePack;
using Orleans;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Friend;

[LaserSerializable]
[GenerateSerializer]
[MessagePackObject]
[Alias("Friend.FriendOnlineStatusEntry")]
public partial class FriendOnlineStatusEntry : LaserContract
{
    [Field(0)] [Id(0)] [Key(0)] public long AccountId { get; set; }

    [Field(1, IsVarInt = true)]
    [Id(1)]
    [Key(1)]
    public int Status { get; set; }

    [Field(2, IsVarInt = true)]
    [Id(2)]
    [Key(2)]
    public int UnkVInt { get; set; }

    [Field(3)] [Id(3)] [Key(3)] public bool InvitesBlocked { get; set; }

    [Field(4, PresenceBool = true)]
    [Id(4)]
    [Key(4)]
    public AllianceTeamEntry? AllianceTeamEntry { get; set; }
}