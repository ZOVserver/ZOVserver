using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class AcceptFriendMessage : PiranhaMessage
{
    [Field(0)] public long AccountId { get; set; }

    public override int GetMessageType()
    {
        return 10501;
    }

    public override int GetServiceNodeType()
    {
        return 26;
    }
}