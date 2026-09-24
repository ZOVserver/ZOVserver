using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class SCIDAccountBoundMessage : PiranhaMessage
{
    [Field(0)] public int ResultCode { get; set; }

    [Field(1, Compressed = true)] public string SupercellIdToken { get; set; } = string.Empty;

    public override int GetMessageType()
    {
        return 25165;
    }

    public override int GetServiceNodeType()
    {
        return 10;
    }
}