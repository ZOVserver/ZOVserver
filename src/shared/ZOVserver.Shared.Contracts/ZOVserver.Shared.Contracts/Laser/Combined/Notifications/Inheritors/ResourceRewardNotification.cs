using MessagePack;
using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Inheritors;

[MessagePackObject]
[LaserSerializable]
public partial class ResourceRewardNotification : BaseNotification
{
    [Key(102)]
    [Field(0, BaseMethodBefore = true)]
    public int UnkVInt { get; set; }

    [Key(100)]
    [Field(1, Type = FieldType.VInt32)]
    public int ResourceGlobalId { get; set; }

    [Key(101)]
    [Field(2, Type = FieldType.VInt32)]
    public int ResourceAmount { get; set; }

    public override int GetNotificationType()
    {
        return 90;
    }
}