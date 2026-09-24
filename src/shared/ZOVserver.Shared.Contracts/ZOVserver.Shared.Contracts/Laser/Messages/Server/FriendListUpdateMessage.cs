using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Friend;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class FriendListUpdateMessage : PiranhaMessage
{
    [Field(0)] public bool UnkBool { get; set; }
    [Field(1)] public FriendEntry? FriendEntry { get; set; }

    public override int GetMessageType()
    {
        return 20106;
    }

    public override int GetServiceNodeType()
    {
        return 26;
    }
}