using MessagePack;
using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Inheritors;

[MessagePackObject]
[LaserSerializable]
public partial class FreeTextNotification : BaseNotification
{
    [Key(100)]
    [Field(0, IsVarInt = true, BaseMethodBefore = true)]
    public int Type { get; set; }

    public override int GetNotificationType()
    {
        return 81;
    }
}