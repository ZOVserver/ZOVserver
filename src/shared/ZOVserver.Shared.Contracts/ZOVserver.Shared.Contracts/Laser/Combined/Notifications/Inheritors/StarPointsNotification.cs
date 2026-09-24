using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Entries;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Inheritors;

[MessagePackObject]
[LaserSerializable]
public partial class StarPointsNotification : BaseNotification
{
    [Key(100)]
    [Field(0, BaseMethodBefore = true)]
    public List<ScoreEntry> ScoreEntries { get; set; } = [];

    public override int GetNotificationType()
    {
        return 79;
    }
}