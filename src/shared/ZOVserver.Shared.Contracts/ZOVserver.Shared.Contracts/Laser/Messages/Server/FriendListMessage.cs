using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Friend;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class FriendListMessage : PiranhaMessage
{
    [Field(0)] public int ListType { get; set; }
    [Field(1)] public bool UnkBool { get; set; } = true; // false = flood
    [Field(2)] public bool FriendRequestsBlocked { get; set; }
    [Field(3, CountIsI32 = true)] public FriendEntry[] FriendEntries { get; set; } = [];

    public override int GetMessageType()
    {
        return 20105;
    }

    public override int GetServiceNodeType()
    {
        return 3;
    }
}