using System.Security.Cryptography;
using Grpc.Core;
using MessagePack;
using Messagesservice;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Laser.Combined.Notifications;
using ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Inheritors;
using ZOVserver.Shared.TitanRemnants.Mathem.Shared;

namespace ZOVserver.AdminPanel.Backend.Services;

public class MessagesService(ILogger<MessagesService> logger) : Messagesservice.MessagesService.MessagesServiceBase
{
    private static int GetRandomInt()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }

    public override async Task<SendMessagesResponse> SendMessages(SendMessagesRequest request,
        ServerCallContext context)
    {
        logger.LogInformation("Sending {c} messages to player {h}-{l}", request.Messages.Count, request.PlayerId.High,
            request.PlayerId.Low);

        var playerId = new LogicLong(request.PlayerId.High, request.PlayerId.Low);
        var home = ClientHelper.GetHomeGrain(playerId);

        var m = new byte[request.Messages.Count][];

        var i = 0;
        foreach (var msg in request.Messages)
        {
            var freeTextNotification = new FreeTextNotification
            {
                NotificationIndex = GetRandomInt(),
                CreationTime = DateTime.UtcNow,
                Message = msg.Text,
                Type = msg.FromSupport ? 1 : 0
            };

            m[i++] = MessagePackSerializer.Serialize<BaseNotification>(freeTextNotification);
        }

        await home.AddNotifications(m);

        return new SendMessagesResponse
        {
            Success = true,
            Message = $"Successfully sent {request.Messages.Count} messages to player"
        };
    }
}