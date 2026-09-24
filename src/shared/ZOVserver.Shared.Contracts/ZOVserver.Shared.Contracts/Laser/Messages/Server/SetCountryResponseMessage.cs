using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class SetCountryResponseMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public int Response { get; set; }
    [Field(1, AsDataRef = true)] public int Country { get; set; }

    public override int GetMessageType()
    {
        return 24178;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}