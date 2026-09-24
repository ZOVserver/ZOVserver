using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class SetAllianceCountryMessage : PiranhaMessage
{
    [Field(0)] public int CountryGlobalId { get; set; }

    public override int GetMessageType()
    {
        return 14299;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}