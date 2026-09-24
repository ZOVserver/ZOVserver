using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class SetSupportedCreatorMessage : PiranhaMessage
{
    [Field(0)] public string Code { get; set; } = string.Empty;

    public override int GetMessageType()
    {
        return 18686;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}