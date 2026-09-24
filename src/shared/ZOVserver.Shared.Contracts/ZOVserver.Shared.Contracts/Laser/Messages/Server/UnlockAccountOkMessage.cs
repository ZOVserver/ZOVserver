using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class UnlockAccountOkMessage : PiranhaMessage
{
    [Field(0)] public long AccountId { get; set; }

    [Field(1)] public string PassToken { get; set; } = string.Empty;

    public override int GetMessageType()
    {
        return 20132;
    }

    public override int GetServiceNodeType()
    {
        return 1;
    }
}