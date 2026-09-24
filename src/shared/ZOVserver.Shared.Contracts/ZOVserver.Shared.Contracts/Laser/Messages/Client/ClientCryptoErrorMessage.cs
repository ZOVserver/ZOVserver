using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class ClientCryptoErrorMessage : PiranhaMessage
{
    [Field(0)] public int A1 { get; set; }

    public override int GetMessageType()
    {
        return 10099;
    }

    public override int GetServiceNodeType()
    {
        return 1;
    }
}