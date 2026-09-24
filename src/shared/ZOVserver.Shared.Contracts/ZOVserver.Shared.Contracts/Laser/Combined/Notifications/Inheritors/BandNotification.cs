using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Inheritors;

[MessagePackObject]
[LaserSerializable]
public partial class BandNotification : BaseNotification
{
    [Key(100)]
    [Field(0, BaseMethodBefore = true)]
    public PlayerDisplayData? PlayerDisplayData { get; set; }

    public override int GetNotificationType()
    {
        return 82;
    }
}