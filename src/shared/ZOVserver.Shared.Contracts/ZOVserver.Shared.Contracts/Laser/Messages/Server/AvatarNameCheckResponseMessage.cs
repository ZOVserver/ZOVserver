using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class AvatarNameCheckResponseMessage : PiranhaMessage
{
    [Field(0)] public bool ReasonIsNotNull { get; set; }

    [Field(1)] public int Reason { get; set; }

    [Field(2)] public string? UnkString { get; set; }

    public override int GetMessageType()
    {
        return 20300;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}