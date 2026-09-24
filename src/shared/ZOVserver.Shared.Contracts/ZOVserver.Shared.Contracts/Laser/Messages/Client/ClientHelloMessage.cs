using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class ClientHelloMessage : PiranhaMessage
{
    [Field(0)] public int A1 { get; set; }
    [Field(1)] public int A2 { get; set; }
    [Field(2)] public int A3 { get; set; }
    [Field(3)] public int A4 { get; set; }
    [Field(4)] public int A5 { get; set; }
    [Field(5)] public string A6 { get; set; } = string.Empty;
    [Field(6)] public int A7 { get; set; }
    [Field(7)] public int A8 { get; set; }

    public override int GetMessageType()
    {
        return 10100;
    }

    public override int GetServiceNodeType()
    {
        return 1;
    }
}