using MessagePack;
using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Inheritors;

[MessagePackObject]
[LaserSerializable]
public partial class GemRewardNotification : BaseNotification
{
    [Key(101)]
    [Field(0, BaseMethodBefore = true)]
    public int UnkVInt { get; set; }

    [Key(100)] [Field(1, IsVarInt = true)] public int Gems { get; set; }

    public override int GetNotificationType()
    {
        return 89;
    }
}