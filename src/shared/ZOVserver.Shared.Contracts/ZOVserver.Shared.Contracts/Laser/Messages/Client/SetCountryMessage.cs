using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class SetCountryMessage : PiranhaMessage
{
    [Field(0, AsDataRef = true)] public int Country { get; set; }

    public override int GetMessageType()
    {
        return 12998;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}