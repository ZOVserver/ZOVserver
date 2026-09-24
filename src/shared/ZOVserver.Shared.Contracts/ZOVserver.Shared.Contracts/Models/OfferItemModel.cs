using Newtonsoft.Json;

namespace ZOVserver.Shared.Contracts.Models;

public class OfferItemModel
{
    [JsonProperty("item_type")] public int ItemType { get; set; }

    [JsonProperty("quantity")] public int Count { get; set; }

    [JsonProperty("brawler_name")] public string BrawlerName { get; set; } = string.Empty;

    [JsonProperty("skin_name")] public string SkinName { get; set; } = string.Empty;
}