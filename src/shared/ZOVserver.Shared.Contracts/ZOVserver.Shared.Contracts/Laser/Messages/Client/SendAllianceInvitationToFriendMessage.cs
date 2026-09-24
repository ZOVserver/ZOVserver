using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class SendAllianceInvitationToFriendMessage : PiranhaMessage
{
    [Field(0)] public long AvatarId { get; set; }
    [Field(1)] public string FacebookId { get; set; } = string.Empty;
    [Field(2)] public string GamecenterId { get; set; } = string.Empty;

    public override int GetMessageType()
    {
        return 14326;
    }

    public override int GetServiceNodeType()
    {
        return 26;
    }
}