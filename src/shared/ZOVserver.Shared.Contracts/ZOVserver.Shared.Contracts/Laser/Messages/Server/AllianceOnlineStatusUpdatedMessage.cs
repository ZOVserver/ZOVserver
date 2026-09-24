using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class AllianceOnlineStatusUpdatedMessage : PiranhaMessage
{
    [Field(0)] public int UnkVInt { get; set; }
    [Field(1)] public StatusChangeEntry[] StatusChangeEntries { get; set; } = [];

    public override int GetMessageType()
    {
        return 20207;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}