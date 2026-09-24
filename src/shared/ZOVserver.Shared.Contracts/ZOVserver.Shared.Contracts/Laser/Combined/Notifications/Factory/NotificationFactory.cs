using ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Inheritors;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Factory;

public static class NotificationFactory
{
    public static BaseNotification CreateNotificationByType(int type)
    {
        return type switch
        {
            78 => new RankRewardNotification(),
            79 => new StarPointsNotification(),
            80 => new StarPointMigrationNotification(),
            81 => new FreeTextNotification(),
            82 => new BandNotification(),
            83 => new PromoPopupNotification(),
            84 => new StarPowerRewardNotification(),
            88 => new CoinDoublerRewardNotification(),
            89 => new GemRewardNotification(),
            90 => new ResourceRewardNotification(),
            91 => new TicketRewardNotification(),
            92 => new HeroPowerRewardNotification(),
            93 => new HeroRewardNotification(),
            94 => new SkinRewardNotification(),
            _ => throw new ArgumentException($"Unknown notification type: {type}")
        };
    }
}