using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class SCIDBindAccountMessage : PiranhaMessage
{
    [Field(0, Compressed = true)] public string BindToken { get; set; } = string.Empty;

    [Field(1, Compressed = true)] public string SupercellIdToken { get; set; } = string.Empty;

    public override int GetMessageType()
    {
        return 10636;
    }

    public override int GetServiceNodeType()
    {
        return 1;
    }
}