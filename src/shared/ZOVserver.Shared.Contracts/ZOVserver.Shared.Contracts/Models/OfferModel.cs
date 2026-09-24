using Newtonsoft.Json;

namespace ZOVserver.Shared.Contracts.Models;

public class OfferModel
{
    [JsonProperty("id")] public Guid Id { get; set; }

    [JsonProperty("name")] public string Name { get; set; } = string.Empty;

    [JsonProperty("is_daily")] public bool IsDaily { get; set; }

    [JsonProperty("start_time")] public DateTime StartTime { get; set; }

    [JsonProperty("end_time")] public DateTime EndTime { get; set; }

    [JsonProperty("price")] public int Price { get; set; }

    [JsonProperty("old_price")] public int OldPrice { get; set; }

    [JsonProperty("price_type")] public int PriceType { get; set; }

    [JsonProperty("background")] public string Background { get; set; } = string.Empty;

    [JsonProperty("show_to_new_users")] public bool ShowToNewUsers { get; set; }

    [JsonProperty("items")] public List<OfferItemModel> Items { get; set; } = [];
}