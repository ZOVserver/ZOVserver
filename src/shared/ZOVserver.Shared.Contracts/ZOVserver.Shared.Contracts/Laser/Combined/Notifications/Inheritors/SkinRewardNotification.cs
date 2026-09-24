using MessagePack;
using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Inheritors;

[MessagePackObject]
[LaserSerializable]
public partial class SkinRewardNotification : BaseNotification
{
    [Key(100)]
    [Field(0, BaseMethodBefore = true, Type = FieldType.VInt32)]
    public int SkinGlobalId { get; set; }

    public override int GetNotificationType()
    {
        return 94;
    }
}