using System.Collections.Concurrent;
using dotnet_etcd;
using Grpc.Core;
using Mvccpb;
using Newtonsoft.Json;
using NLog;
using ZOVserver.Shared.Contracts.Models;

namespace ZOVserver.Services.Game.HomeService.Manager;

public static class OffersManager
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly ConcurrentDictionary<Guid, OfferModel> OffersToAdd = [];
    private static readonly ConcurrentDictionary<Guid, bool> OffersToRemove = [];

    private static EtcdClient _etcdClient = null!;
    private static string _prefix = null!;

    private static CancellationTokenSource? _watchCts;

    public static async Task InitializeAsync(string connectionString, string prefix)
    {
        var isHttps = connectionString.StartsWith("https", StringComparison.OrdinalIgnoreCase);

        _etcdClient = new EtcdClient(connectionString, configureChannelOptions: options =>
        {
            if (isHttps)
            {
                options.Credentials = ChannelCredentials.SecureSsl;

                var httpHandler = new HttpClientHandler();

#if DEBUG
                httpHandler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#endif

                options.HttpHandler = httpHandler;
            }
            else
            {
                options.Credentials = ChannelCredentials.Insecure;
            }
        });

        _prefix = prefix;

        try
        {
            await LoadInitialOffersAsync();
            StartWatching();
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Failed to initialize OffersManager");
            throw;
        }
    }

    private static async Task LoadInitialOffersAsync()
    {
        try
        {
            var response = await _etcdClient.GetRangeAsync(_prefix);

            OffersToAdd.Clear();

            foreach (var kv in response.Kvs)
                ProcessOfferUpdate(kv.Value.ToStringUtf8());

            Logger.Info($"Synced {OffersToAdd.Count} offers from etcd.");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error during etcd initial load");
        }
    }

    private static void StartWatching()
    {
        _watchCts?.Cancel();
        _watchCts?.Dispose();
        _watchCts = new CancellationTokenSource();

        var token = _watchCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await _etcdClient.WatchRangeAsync(_prefix, response =>
                {
                    foreach (var @event in response.Events)
                    {
                        var kv = @event.Kv;
                        var key = kv.Key.ToStringUtf8();

                        switch (@event.Type)
                        {
                            case Event.Types.EventType.Put:
                                ProcessOfferUpdate(kv.Value.ToStringUtf8());
                                break;
                            case Event.Types.EventType.Delete:
                                if (Guid.TryParse(key.Replace(_prefix, ""), out var id))
                                    HandleRemoval(id);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }, cancellationToken: token);
            }
            catch (Exception ex)
            {
                Logger.Error($"Etcd watch stream interrupted: {ex.Message}. Retrying...");
                await HandleReconnectAsync();
            }
        }, token);

        _ = Task.Run(async () =>
        {
            while (true)
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), token);

                    await LoadInitialOffersAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Background offers sync failed");
                }
        }, token);
    }

    private static async Task HandleReconnectAsync()
    {
        await Task.Delay(5000);

        try
        {
            await LoadInitialOffersAsync();
            StartWatching();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to reconnect to etcd");
            await HandleReconnectAsync();
        }
    }

    private static void ProcessOfferUpdate(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            var offer = JsonConvert.DeserializeObject<OfferModel>(json);

            if (offer == null)
                return;

            if (DateTime.UtcNow > offer.EndTime)
                return;

            OffersToAdd.AddOrUpdate(offer.Id, offer, (_, _) => offer);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "failed to process offer update");
        }
    }

    private static void HandleRemoval(Guid id)
    {
        OffersToRemove.TryAdd(id, true);
        OffersToAdd.TryRemove(id, out _);
    }

    public static IReadOnlyDictionary<Guid, OfferModel> GetOffersToAdd()
    {
        return OffersToAdd;
    }

    public static IReadOnlyDictionary<Guid, bool> GetOffersToRemove()
    {
        return OffersToRemove;
    }
}