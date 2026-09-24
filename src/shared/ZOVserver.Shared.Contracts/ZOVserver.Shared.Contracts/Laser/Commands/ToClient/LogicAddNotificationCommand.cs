using ZOVserver.Shared.Contracts.Laser.Combined.Notifications;
using ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Factory;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Shared.Contracts.Laser.Commands.ToClient;

public class LogicAddNotificationCommand : LogicServerCommand
{
    public BaseNotification? Notification { get; set; }

    public override void Encode(ByteStream stream)
    {
    }

    public override void Decode(ByteStream stream)
    {
    }

    public override void CustomEncode(ByteStream byteStream)
    {
        if (byteStream.WriteBoolean(Notification != null))
        {
            byteStream.WriteVInt32(Notification!.GetNotificationType());
            Notification.Encode(byteStream);
        }

        base.Encode(byteStream);
    }

    public override void CustomDecode(ByteStream stream)
    {
        if (!stream.ReadBoolean())
        {
            base.Decode(stream);
            return;
        }

        var type = stream.ReadVInt32();

        var notification = NotificationFactory.CreateNotificationByType(type);
        notification.Decode(stream);

        Notification = notification;

        base.Decode(stream);
    }

    public override int GetCommandType()
    {
        return 206;
    }
}