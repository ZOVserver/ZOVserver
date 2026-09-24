using MessagePack;
using ZOVserver.Shared.Contracts.Laser.Combined.Friend;

namespace ZOVserver.Services.Game.FriendshipService.States;

[MessagePackObject]
public class FriendshipState
{
    [IgnoreMember] public int MyFriendRequestsCount;

    [Key(0)] public long AccountId { get; set; }

    [Key(1)] public DateTime FriendshipCreatedTime { get; set; }

    [Key(2)] public Guid SessionId { get; set; }

    [Key(3)] public int GameState { get; set; }

    [Key(4)] public DateTime LastKeepAliveReceivedTime { get; set; }

    [Key(5)] public Dictionary<long, FriendEntry> Friends { get; set; } = [];

    [Key(6)] public FriendEntry? MyFriendEntry { get; set; }

    [Key(7)] public bool FriendRequestsBlocked { get; set; }

    [Key(8)] public Dictionary<long, SuggestionEntry> Suggestions { get; set; } = [];

    [IgnoreMember] public FriendOnlineStatusEntry? CachedOnlineStatusEntry { get; set; }
}