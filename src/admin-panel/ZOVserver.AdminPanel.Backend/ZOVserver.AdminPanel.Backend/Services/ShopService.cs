using Grpc.Core;
using Shopservice;
using ZOVserver.Shared.Contracts.Models;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.AdminPanel.Backend.Services;

public class ShopService(ShopOffersClient offersClient) : Shopservice.ShopService.ShopServiceBase
{
    public override async Task<CreatePromotionResponse> CreatePromotion(CreatePromotionRequest request,
        ServerCallContext context)
    {
        if (request.StartTime > request.EndTime)
            return new CreatePromotionResponse
            {
                Success = false,
                Message = "Invalid time range."
            };

        foreach (var item in request.Items)
            switch (item.ItemType)
            {
                case 3 when string.IsNullOrEmpty(item.BrawlerName):
                    return new CreatePromotionResponse
                    {
                        Success = false,
                        Message = "Brawler name is missing."
                    };
                case 3:
                {
                    var d = LogicDataTables.GetDataByName(16, item.BrawlerName);

                    if (d == null)
                        return new CreatePromotionResponse
                        {
                            Success = false,
                            Message = $"Brawler '{item.BrawlerName}' not found."
                        };

                    break;
                }
                case 4 when string.IsNullOrEmpty(item.SkinName):
                    return new CreatePromotionResponse
                    {
                        Success = false,
                        Message = "Skin name is missing."
                    };
                case 4:
                {
                    var d = LogicDataTables.GetDataByName(29, item.SkinName);

                    if (d == null)
                        return new CreatePromotionResponse
                        {
                            Success = false,
                            Message = $"Skin '{item.SkinName}' not found."
                        };

                    break;
                }
            }

        var offer = new OfferModel
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            IsDaily = request.IsDaily,
            StartTime = DateTimeOffset.FromUnixTimeSeconds(request.StartTime).DateTime,
            EndTime = DateTimeOffset.FromUnixTimeSeconds(request.EndTime).DateTime,
            Price = (int)request.Price,
            OldPrice = (int)request.OldPrice,
            PriceType = request.PriceType,
            Background = request.Background,
            ShowToNewUsers = request.ShowToNewUsers,
            Items = request.Items.Select(item => new OfferItemModel
            {
                ItemType = item.ItemType,
                Count = item.Quantity,
                BrawlerName = item.BrawlerName,
                SkinName = item.SkinName
            }).ToList()
        };

        try
        {
            await offersClient.CreateOfferAsync(offer);
        }
        catch (Exception ex)
        {
            return new CreatePromotionResponse { Success = false, Message = $"Failed to save to etcd: {ex.Message}" };
        }

        return new CreatePromotionResponse
        {
            Success = true,
            Message = $"Promotion '{request.Name}' created successfully.",
            PromotionId = offer.Id.ToString()
        };
    }

    public override async Task<DeletePromotionResponse> DeletePromotion(DeletePromotionRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.PromotionId, out var id))
            return new DeletePromotionResponse { Success = false, Message = "Invalid promotion ID format." };

        var deleted = await offersClient.DeleteOfferAsync(id);

        return new DeletePromotionResponse
        {
            Success = deleted,
            Message = deleted
                ? $"Promotion {request.PromotionId} deleted successfully."
                : $"Promotion {request.PromotionId} not found in etcd."
        };
    }

    public override async Task<GetPromotionHistoryResponse> GetPromotionHistory(GetPromotionHistoryRequest request,
        ServerCallContext context)
    {
        try
        {
            var offers = await offersClient.GetAllOffersAsync();

            var response = new GetPromotionHistoryResponse
            {
                Success = true,
                Message = "Active promotions retrieved successfully from etcd."
            };

            foreach (var offer in offers)
                response.Promotions.Add(new PromotionHistoryEntry
                {
                    PromotionId = offer.Id.ToString(),
                    Name = offer.Name,
                    CreatedAt = new DateTimeOffset(offer.StartTime).ToUnixTimeSeconds(),
                    StartTime = new DateTimeOffset(offer.StartTime).ToUnixTimeSeconds(),
                    EndTime = new DateTimeOffset(offer.EndTime).ToUnixTimeSeconds()
                });

            return response;
        }
        catch (Exception ex)
        {
            return new GetPromotionHistoryResponse { Success = false, Message = ex.Message };
        }
    }
}