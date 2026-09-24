using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class GoHomeFromOfflinePractiseMessage : PiranhaMessage
{
    [Field(0)] public bool UnkBool { get; set; }

    public override int GetMessageType()
    {
        return 14109;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}