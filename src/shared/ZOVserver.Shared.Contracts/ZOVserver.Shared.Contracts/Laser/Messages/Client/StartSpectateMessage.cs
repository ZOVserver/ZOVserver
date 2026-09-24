using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class StartSpectateMessage : PiranhaMessage
{
    [Field(0)] public long AccountId { get; set; }
    [Field(1)] public bool UnkBool { get; set; }

    public override int GetMessageType()
    {
        return 14104;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}