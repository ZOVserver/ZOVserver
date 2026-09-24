using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Friend;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class FriendOnlineStatusEntryMessage : PiranhaMessage
{
    [Field(0)] public long AccountId { get; set; }
    [Field(1, PresenceBool = true)] public FriendOnlineStatusEntry? FriendOnlineStatusEntry { get; set; }

    public override int GetMessageType()
    {
        return 24555;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}