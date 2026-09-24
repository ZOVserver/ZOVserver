using System.Security.Cryptography;
using Giftsservice;
using Grpc.Core;
using MessagePack;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Laser.Combined.Notifications;
using ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Inheritors;
using ZOVserver.Shared.TitanRemnants.Mathem.Shared;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.AdminPanel.Backend.Services;

public class GiftsService(ILogger<GiftsService> logger) : Giftsservice.GiftsService.GiftsServiceBase
{
    private static int GetRandomInt()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }

    public override async Task<SendGiftResponse> SendGift(SendGiftRequest request, ServerCallContext context)
    {
        logger.LogInformation("Sending gift to player {h}-{l}, reason: {r}", request.PlayerId.High,
            request.PlayerId.Low, request.Reason);

        foreach (var item in request.Items)
        {
            var itemInfo = $"Item Type: {item.ItemType}, Quantity: {item.Quantity}";
            if (!string.IsNullOrEmpty(item.BrawlerName))
                itemInfo += $", Brawler: {item.BrawlerName}";
            if (!string.IsNullOrEmpty(item.SkinName))
                itemInfo += $", Skin: {item.SkinName}";

            logger.LogInformation("Gift item: {info}", itemInfo);
        }

        if (request.Items.Count == 0)
            return new SendGiftResponse
            {
                Success = false,
                Message = "Gift must contain at least one item"
            };

        foreach (var item in request.Items)
        {
            if (item.ItemType == GiftItemType.Unspecified)
                return new SendGiftResponse
                {
                    Success = false,
                    Message = "Item type cannot be unspecified"
                };

            if (item.ItemType != GiftItemType.Brawler && item.ItemType != GiftItemType.BrawlerSkin)
                if (item.Quantity <= 0)
                    return new SendGiftResponse
                    {
                        Success = false,
                        Message = "Quantity must be greater than 0 for this item type"
                    };

            switch (item.ItemType)
            {
                case GiftItemType.Brawler:
                {
                    if (string.IsNullOrEmpty(item.BrawlerName))
                        return new SendGiftResponse
                        {
                            Success = false,
                            Message = "Brawler name is required for brawler items"
                        };

                    var d = LogicDataTables.GetDataByName(16, item.BrawlerName);
                    if (d == null)
                        return new SendGiftResponse
                        {
                            Success = false,
                            Message = $"Brawler '{item.BrawlerName}' not found."
                        };

                    break;
                }
                case GiftItemType.BrawlerSkin:
                {
                    if (string.IsNullOrEmpty(item.SkinName))
                        return new SendGiftResponse
                        {
                            Success = false,
                            Message = "Skin name is required for skin items"
                        };

                    var d = LogicDataTables.GetDataByName(29, item.SkinName);
                    if (d == null)
                        return new SendGiftResponse
                        {
                            Success = false,
                            Message = $"Skin '{item.SkinName}' not found."
                        };

                    break;
                }
            }
        }

        var playerId = new LogicLong(request.PlayerId.High, request.PlayerId.Low);
        var home = ClientHelper.GetHomeGrain(playerId);

        var m = new byte[request.Items.Count][];

        var i = 0;
        foreach (var item in request.Items)
        {
            BaseNotification? notification = null;

            switch (item.ItemType)
            {
                case GiftItemType.Gold:
                {
                    notification = new ResourceRewardNotification
                    {
                        NotificationIndex = GetRandomInt(),
                        CreationTime = DateTime.UtcNow,
                        Message = "🎁",
                        ResourceGlobalId = 5_000_008,
                        ResourceAmount = item.Quantity
                    };

                    break;
                }
                case GiftItemType.Diamonds:
                {
                    notification = new GemRewardNotification
                    {
                        NotificationIndex = GetRandomInt(),
                        CreationTime = DateTime.UtcNow,
                        Message = "🎁",
                        Gems = item.Quantity
                    };

                    break;
                }
                case GiftItemType.Tickets:
                {
                    notification = new TicketRewardNotification
                    {
                        NotificationIndex = GetRandomInt(),
                        CreationTime = DateTime.UtcNow,
                        Message = "🎁",
                        Tickets = item.Quantity
                    };

                    break;
                }
                case GiftItemType.TokenDoubler:
                {
                    notification = new CoinDoublerRewardNotification
                    {
                        NotificationIndex = GetRandomInt(),
                        CreationTime = DateTime.UtcNow,
                        Message = "🎁",
                        TokenDoublers = item.Quantity
                    };

                    break;
                }
                case GiftItemType.StarPoints:
                {
                    notification = new ResourceRewardNotification
                    {
                        NotificationIndex = GetRandomInt(),
                        CreationTime = DateTime.UtcNow,
                        Message = "🎁",
                        ResourceGlobalId = 5_000_010,
                        ResourceAmount = item.Quantity
                    };

                    break;
                }
                case GiftItemType.Brawler:
                {
                    notification = new HeroRewardNotification
                    {
                        NotificationIndex = GetRandomInt(),
                        CreationTime = DateTime.UtcNow,
                        Message = "🎁",
                        HeroGlobalId = LogicDataTables.GetDataByName(16, item.BrawlerName)!.GlobalId
                    };

                    break;
                }
                case GiftItemType.BrawlerSkin:
                {
                    notification = new SkinRewardNotification
                    {
                        NotificationIndex = GetRandomInt(),
                        CreationTime = DateTime.UtcNow,
                        Message = "🎁",
                        SkinGlobalId = LogicDataTables.GetDataByName(29, item.SkinName)!.GlobalId
                    };

                    break;
                }
            }

            if (notification == null) continue;
            m[i++] = MessagePackSerializer.Serialize(notification);
        }

        await home.AddNotifications(m);

        return new SendGiftResponse
        {
            Success = true,
            Message =
                $"Successfully sent gift with {request.Items.Count} items to player {request.PlayerId.High}-{request.PlayerId.Low}"
        };
    }
}