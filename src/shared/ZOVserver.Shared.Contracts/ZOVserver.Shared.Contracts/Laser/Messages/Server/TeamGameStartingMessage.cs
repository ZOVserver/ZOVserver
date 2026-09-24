using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class TeamGameStartingMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public int Unk1 { get; set; }
    [Field(1, IsVarInt = true)] public int Unk2 { get; set; }
    [Field(2, AsDataRef = true)] public int LocationGlobalId { get; set; }

    public override int GetMessageType()
    {
        return 24130;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}