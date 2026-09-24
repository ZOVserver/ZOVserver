using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class AvatarNameChangeFailedMessage : PiranhaMessage
{
    [Field(0)] public int Reason { get; set; }

    public override int GetMessageType()
    {
        return 20205;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}