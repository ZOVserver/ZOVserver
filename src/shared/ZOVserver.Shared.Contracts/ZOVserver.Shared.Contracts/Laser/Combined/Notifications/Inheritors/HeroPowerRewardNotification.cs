using MessagePack;
using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Inheritors;

[MessagePackObject]
[LaserSerializable]
public partial class HeroPowerRewardNotification : BaseNotification
{
    [Key(102)]
    [Field(0, BaseMethodBefore = true)]
    public int UnkVInt { get; set; }

    [Key(100)]
    [Field(1, Type = FieldType.VInt32)]
    public int BrawlerGlobalId { get; set; }

    [Key(101)] [Field(2, IsVarInt = true)] public int PowerPoints { get; set; }

    public override int GetNotificationType()
    {
        return 92;
    }
}