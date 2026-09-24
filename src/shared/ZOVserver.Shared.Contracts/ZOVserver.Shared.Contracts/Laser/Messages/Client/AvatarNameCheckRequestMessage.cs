using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class AvatarNameCheckRequestMessage : PiranhaMessage
{
    [Field(0)] public string Name { get; set; } = string.Empty;

    public override int GetMessageType()
    {
        return 14600;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}