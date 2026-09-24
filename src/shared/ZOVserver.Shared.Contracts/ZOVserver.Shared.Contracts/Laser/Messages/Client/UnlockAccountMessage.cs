using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class UnlockAccountMessage : PiranhaMessage
{
    [Field(0)] public long AccountId { get; set; }

    [Field(1)] public string PassToken { get; set; } = string.Empty;

    [Field(2)] public string UnlockCode { get; set; } = string.Empty;

    [Field(3)] public string ScIdToken { get; set; } = string.Empty;

    public override int GetMessageType()
    {
        return 10121;
    }

    public override int GetServiceNodeType()
    {
        return 1;
    }
}