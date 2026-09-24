using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Inheritors;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Notifications;

[Union(78, typeof(RankRewardNotification))]
[Union(79, typeof(StarPointsNotification))]
[Union(80, typeof(StarPointMigrationNotification))]
[Union(81, typeof(FreeTextNotification))]
[Union(82, typeof(BandNotification))]
[Union(83, typeof(PromoPopupNotification))]
[Union(84, typeof(StarPowerRewardNotification))]
[Union(88, typeof(CoinDoublerRewardNotification))]
[Union(89, typeof(GemRewardNotification))]
[Union(90, typeof(ResourceRewardNotification))]
[Union(91, typeof(TicketRewardNotification))]
[Union(92, typeof(HeroPowerRewardNotification))]
[Union(93, typeof(HeroRewardNotification))]
[Union(94, typeof(SkinRewardNotification))]
[MessagePackObject]
[LaserSerializable]
public abstract partial class BaseNotification : LaserContract
{
    [Key(0)] [Field(0)] public int NotificationIndex { get; set; }

    [Key(1)]
    [Field(2, CalculateSecondsPassed = true)]
    public DateTime CreationTime { get; set; }

    [Key(2)] [Field(3)] public string Message { get; set; } = string.Empty;

    [Key(3)] [Field(1)] public bool AlreadyRead { get; set; }

    public abstract int GetNotificationType();
}