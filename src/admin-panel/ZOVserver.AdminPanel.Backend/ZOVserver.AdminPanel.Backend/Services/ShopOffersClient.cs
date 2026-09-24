using dotnet_etcd;
using Newtonsoft.Json;
using ZOVserver.Shared.Contracts.Models;

namespace ZOVserver.AdminPanel.Backend.Services;

public class ShopOffersClient(EtcdClient etcdClient, string prefix)
{
    public async Task CreateOfferAsync(OfferModel offer)
    {
        var json = JsonConvert.SerializeObject(offer);
        await etcdClient.PutAsync(prefix + offer.Id, json);
    }

    public async Task<bool> DeleteOfferAsync(Guid id)
    {
        var resp = await etcdClient.DeleteAsync(prefix + id);
        return resp.Deleted > 0;
    }

    public async Task<List<OfferModel>> GetAllOffersAsync()
    {
        var response = await etcdClient.GetRangeAsync(prefix);

        var offers = new List<OfferModel>();

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var kv in response.Kvs)
        {
            var offer = JsonConvert.DeserializeObject<OfferModel>(kv.Value.ToStringUtf8());

            if (offer != null && offer.EndTime > DateTime.UtcNow)
                offers.Add(offer);
        }

        return offers;
    }
}