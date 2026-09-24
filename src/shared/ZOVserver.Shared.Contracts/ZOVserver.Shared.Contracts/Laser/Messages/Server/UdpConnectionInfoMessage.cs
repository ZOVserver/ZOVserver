using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class UdpConnectionInfoMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public int ServerPort { get; set; }
    [Field(1)] public string? ServerIp { get; set; }


    [Field(2)] public int SessionBytesCount { get; set; } = 10;
    [Field(3)] public ulong SessionLow { get; set; }
    [Field(4)] public ushort SessionHigh { get; set; }

    [Field(5)] public byte[] KaNaN { get; set; } = [];

    public override int GetMessageType()
    {
        return 24112;
    }

    public override int GetServiceNodeType()
    {
        return 27;
    }
}