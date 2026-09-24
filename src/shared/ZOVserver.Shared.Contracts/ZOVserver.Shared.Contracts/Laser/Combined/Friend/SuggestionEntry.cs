using MessagePack;
using Orleans;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Friend;

[LaserSerializable]
[GenerateSerializer]
[MessagePackObject]
[Alias("Friend.SuggestionEntry")]
public partial class SuggestionEntry : LaserContract
{
    [Field(0)] [Id(0)] [Key(0)] public int PairedGames { get; set; }
    [Field(1)] [Id(1)] [Key(1)] public FriendEntry? FriendEntry { get; set; }
}