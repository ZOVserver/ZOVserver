using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class CryptoErrorMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public int A1 { get; set; }

    public override int GetMessageType()
    {
        return 29997;
    }

    public override int GetServiceNodeType()
    {
        return 1;
    }
}