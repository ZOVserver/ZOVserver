using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class KickAllianceMemberMessage : PiranhaMessage
{
    [Field(0)] public long MemberId { get; set; }
    [Field(1)] public string Message { get; set; } = string.Empty;

    public override int GetMessageType()
    {
        return 14307;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}