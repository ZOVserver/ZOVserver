using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class OutOfSyncMessage : PiranhaMessage
{
    [Field(0)] public int Unk1VInt { get; set; } = 1;
    [Field(1)] public int Unk2VInt { get; set; }
    [Field(2)] public int Unk3VInt { get; set; }

    public override int GetMessageType()
    {
        return 24104;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}