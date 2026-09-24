using MessagePack;
using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Inheritors;

[MessagePackObject]
[LaserSerializable]
public partial class StarPointMigrationNotification : BaseNotification
{
    [Key(100)]
    [Field(0, BaseMethodBefore = true, IsVarInt = true)]
    public int StarPointsGained { get; set; }

    public override int GetNotificationType()
    {
        return 80;
    }
}