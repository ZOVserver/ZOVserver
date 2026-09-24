using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class MatchmakeRequestMessage : PiranhaMessage
{
    [Field(0, AsDataRef = true)] public int BrawlerGlobalId { get; set; }
    [Field(1, IsVarInt = true)] public int Unk1 { get; set; }
    [Field(2, IsVarInt = true)] public int EventSlot { get; set; }
    [Field(3, IsVarInt = true)] public int Unk2 { get; set; }
    [Field(4, IsVarInt = true)] public int Unk3 { get; set; }

    public override int GetMessageType()
    {
        return 14103;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}