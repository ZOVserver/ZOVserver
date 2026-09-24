using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class AddFriendMessage : PiranhaMessage
{
    [Field(0)] public long AccountId { get; set; }
    [Field(1)] public int Reason { get; set; }
    [Field(2)] public int ReasonDetails { get; set; }

    public override int GetMessageType()
    {
        return 10502;
    }

    public override int GetServiceNodeType()
    {
        return 26;
    }
}