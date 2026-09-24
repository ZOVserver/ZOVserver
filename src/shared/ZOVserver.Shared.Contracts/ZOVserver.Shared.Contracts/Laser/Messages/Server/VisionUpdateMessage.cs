using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class VisionUpdateMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public int Tick { get; set; }
    [Field(1, IsVarInt = true)] public int LastInput { get; set; }
    [Field(2, IsVarInt = true)] public int Unk1 { get; set; }
    [Field(3, IsVarInt = true)] public int Viewers { get; set; }
    [Field(4)] public bool BrawlTvMode { get; set; }
    [Field(5)] public byte[]? BitStreamBuffer { get; set; }

    public override int GetMessageType()
    {
        return 24109;
    }

    public override int GetServiceNodeType()
    {
        return 4;
    }
}