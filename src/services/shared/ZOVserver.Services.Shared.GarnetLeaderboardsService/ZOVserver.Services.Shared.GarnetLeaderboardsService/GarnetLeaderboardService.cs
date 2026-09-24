using StackExchange.Redis;
using ZOVserver.Shared.Abstractions;

namespace ZOVserver.Services.Shared.GarnetLeaderboardsService;

public class GarnetLeaderboardService : ILeaderboardService
{
    private const string GlobalKey = "g";
    private const string RegionKey = "r";
    private const string PlayerSuffix = "players";
    private const string BrawlerSuffix = "brawler";
    private const string AllianceSuffix = "alliances";

    private readonly IDatabase _db;

    public GarnetLeaderboardService(GarnetConfig config)
    {
        var options = new ConfigurationOptions
        {
            AllowAdmin = config.AllowAdmin,
            ConnectRetry = config.ConnectRetry,
            AbortOnConnectFail = config.AbortOnConnectFail,
            ReconnectRetryPolicy = new ExponentialRetry(config.MinRetryBackoff, config.MaxRetryBackoff),
            KeepAlive = config.KeepAlive,
            Password = config.Password
        };

        var endpoints = config.ConnectionString?.Split(',') ??
                        throw new ArgumentException("Invalid connection string.");

        foreach (var endpoint in endpoints)
        {
            var parts = endpoint.Trim().Split(':');

            if (parts.Length == 2 && int.TryParse(parts[1], out var port))
                options.EndPoints.Add(parts[0], port);
            else
                throw new ArgumentException("Invalid endpoint format. Use 'host:port'.");
        }

        _db = ConnectionMultiplexer.Connect(options).GetDatabase();
    }

    public async Task UpdatePlayerFullDataAsync(long accountId, string? oldRegion, string? newRegion,
        Dictionary<int, int> brawlerTrophies)
    {
        var batch = _db.CreateBatch();
        var idStr = accountId.ToString();
        var totalTrophies = brawlerTrophies.Values.Sum();

        // ReSharper disable once UseObjectOrCollectionInitializer
        var tasks = new List<Task>();

        if (true)
        {
            tasks.Add(batch.SortedSetAddAsync($"{GlobalKey}_{PlayerSuffix}", idStr, totalTrophies));

            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var bt in brawlerTrophies)
                tasks.Add(batch.SortedSetAddAsync($"{GlobalKey}_{BrawlerSuffix}_{bt.Key}", idStr, bt.Value));
        }

        if (!string.IsNullOrEmpty(oldRegion) && oldRegion != newRegion)
        {
            tasks.Add(batch.SortedSetRemoveAsync($"{RegionKey}_{oldRegion}_{PlayerSuffix}", idStr));

            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var bId in brawlerTrophies.Keys)
                tasks.Add(batch.SortedSetRemoveAsync($"{RegionKey}_{oldRegion}_{BrawlerSuffix}_{bId}", idStr));
        }

        if (!string.IsNullOrEmpty(newRegion))
        {
            tasks.Add(batch.SortedSetAddAsync($"{RegionKey}_{newRegion}_{PlayerSuffix}", idStr, totalTrophies));

            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var bt in brawlerTrophies)
                tasks.Add(batch.SortedSetAddAsync($"{RegionKey}_{newRegion}_{BrawlerSuffix}_{bt.Key}", idStr,
                    bt.Value));
        }

        batch.Execute();
        await Task.WhenAll(tasks);
    }

    public async Task UpdateAllianceRegionAsync(long allianceId, string? oldRegion, string? newRegion, int trophies)
    {
        var batch = _db.CreateBatch();
        var idStr = allianceId.ToString();

        // ReSharper disable once UseObjectOrCollectionInitializer
        var tasks = new List<Task>();

        tasks.Add(batch.SortedSetAddAsync($"{GlobalKey}_{AllianceSuffix}", idStr, trophies));

        if (!string.IsNullOrEmpty(oldRegion) && oldRegion != newRegion)
            tasks.Add(batch.SortedSetRemoveAsync($"{RegionKey}_{oldRegion}_{AllianceSuffix}", idStr));

        if (!string.IsNullOrEmpty(newRegion))
            tasks.Add(batch.SortedSetAddAsync($"{RegionKey}_{newRegion}_{AllianceSuffix}", idStr, trophies));

        batch.Execute();
        await Task.WhenAll(tasks);
    }

    public async Task UpdatePlayerTrophiesAsync(long accountId, int totalTrophies, int brawlerId, int brawlerTrophies,
        string? region)
    {
        var batch = _db.CreateBatch();
        var idStr = accountId.ToString();

        // ReSharper disable once UseObjectOrCollectionInitializer
        var tasks = new List<Task>();

        tasks.Add(batch.SortedSetAddAsync($"{GlobalKey}_{PlayerSuffix}", idStr, totalTrophies));
        tasks.Add(batch.SortedSetAddAsync($"{GlobalKey}_{BrawlerSuffix}_{brawlerId}", idStr, brawlerTrophies));

        if (!string.IsNullOrEmpty(region))
        {
            tasks.Add(batch.SortedSetAddAsync($"{RegionKey}_{region}_{PlayerSuffix}", idStr, totalTrophies));
            tasks.Add(batch.SortedSetAddAsync($"{RegionKey}_{region}_{BrawlerSuffix}_{brawlerId}", idStr,
                brawlerTrophies));
        }

        batch.Execute();
        await Task.WhenAll(tasks);
    }

    public async Task UpdateAllianceTrophiesAsync(long allianceId, int trophies, string? region)
    {
        var batch = _db.CreateBatch();
        var idStr = allianceId.ToString();

        // ReSharper disable once UseObjectOrCollectionInitializer
        var tasks = new List<Task>();

        tasks.Add(batch.SortedSetAddAsync($"{GlobalKey}_{AllianceSuffix}", idStr, trophies));

        if (!string.IsNullOrEmpty(region))
            tasks.Add(batch.SortedSetAddAsync($"{RegionKey}_{region}_{AllianceSuffix}", idStr, trophies));

        batch.Execute();
        await Task.WhenAll(tasks);
    }

    public Task<List<ILeaderboardEntry>> GetTopPlayersAsync(string? region = null, int count = 200)
    {
        var key = string.IsNullOrEmpty(region)
            ? $"{GlobalKey}_{PlayerSuffix}"
            : $"{RegionKey}_{region}_{PlayerSuffix}";

        return GetTopInternalAsync(key, count);
    }

    public Task<List<ILeaderboardEntry>> GetTopBrawlersAsync(int brawlerId, string? region = null, int count = 200)
    {
        var key = string.IsNullOrEmpty(region)
            ? $"{GlobalKey}_{BrawlerSuffix}_{brawlerId}"
            : $"{RegionKey}_{region}_{BrawlerSuffix}_{brawlerId}";

        return GetTopInternalAsync(key, count);
    }

    public Task<List<ILeaderboardEntry>> GetTopAlliancesAsync(string? region = null, int count = 200)
    {
        var key = string.IsNullOrEmpty(region)
            ? $"{GlobalKey}_{AllianceSuffix}"
            : $"{RegionKey}_{region}_{AllianceSuffix}";

        return GetTopInternalAsync(key, count);
    }

    private async Task<List<ILeaderboardEntry>> GetTopInternalAsync(string key, int count)
    {
        var result = await _db.SortedSetRangeByRankWithScoresAsync(key, 0, count - 1, Order.Descending);

        if (result.Length == 0)
            return [];

        var entries = new List<ILeaderboardEntry>(result.Length);

        foreach (var item in result)
            if (item.Element.TryParse(out long id))
                entries.Add(new LeaderboardEntry
                {
                    Id = id,
                    Trophies = (int)item.Score
                });

        return entries;
    }
}