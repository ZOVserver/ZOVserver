using ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA;
using ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;
using ZOVserver.Shared.Contracts.Laser.Combined.Notifications;
using ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Factory;
using ZOVserver.Shared.TitanRemnants.Streams;
using ZOVserver.Shared.TitanRemnants.Streams.Helper;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Home;

public class LogicClientHome
{
    public LogicDailyData? DailyData { get; set; }
    public LogicConfData? ConfData { get; set; }

    public long HomeId { get; set; }

    public Dictionary<int, BaseNotification> Notifications { get; set; } = [];

    public void CustomEncode(ByteStream byteStream)
    {
        DailyData?.Encode(byteStream);
        ConfData?.Encode(byteStream);

        byteStream.WriteI64(HomeId); // this + 8

        byteStream.WriteVInt32(Notifications.Count);
        foreach (var notification in Notifications.Values.Reverse())
        {
            byteStream.WriteVInt32(notification.GetNotificationType());
            notification.Encode(byteStream);
        }

        byteStream.WriteVInt32(0); // this + 44
        byteStream.WriteBoolean(false); // this + 40

        var inlThis36P8 = byteStream.WriteVInt32(0); // this + 36 + 8
        for (var v9 = 0; v9 < inlThis36P8; v9++)
            new GatchaDrop().Encode(byteStream); // this36[v9]

        var result = byteStream.WriteVInt32(0); // this + 68
        for (var i = 0; i < result; i++)
            ByteStreamHelper.WriteDataReference(byteStream, 0); // this60[i]
    }

    public void CustomDecode(ByteStream byteStream)
    {
        DailyData = new LogicDailyData();
        DailyData.Decode(byteStream);

        ConfData = new LogicConfData();
        ConfData.Decode(byteStream);

        HomeId = byteStream.ReadI64(); // this + 8

        var notificationsCount = byteStream.ReadVInt32();
        Notifications = new Dictionary<int, BaseNotification>();
        for (var i = 0; i < notificationsCount; i++)
        {
            var notificationType = byteStream.ReadVInt32();
            var notification = NotificationFactory.CreateNotificationByType(notificationType);
            notification.Decode(byteStream);
            Notifications.Add(i, notification);
        }

        _ = byteStream.ReadVInt32(); // this + 44
        _ = byteStream.ReadBoolean(); // this + 40


        var gatchaDropsCount = byteStream.ReadVInt32(); // this + 36 + 8
        var gd = new List<GatchaDrop>();
        for (var i = 0; i < gatchaDropsCount; i++)
        {
            var drop = new GatchaDrop();
            drop.Decode(byteStream);
            gd.Add(drop);
        }

        var dataRefCount = byteStream.ReadVInt32(); // this + 68
        var dataReferences = new List<int>();
        for (var i = 0; i < dataRefCount; i++)
        {
            var dataRef = ByteStreamHelper.ReadDataReference(byteStream);
            dataReferences.Add(dataRef);
        }
    }
}