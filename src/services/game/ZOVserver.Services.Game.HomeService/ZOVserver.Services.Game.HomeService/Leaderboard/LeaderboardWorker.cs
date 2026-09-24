using System.Collections.Concurrent;
using MessagePack;
using Microsoft.Extensions.Hosting;
using NLog;
using StackExchange.Redis;
using ZOVserver.Shared.Contracts.Models;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Services.Game.HomeService.Leaderboard;

public class LeaderboardWorker : BackgroundService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static Tuple<PlayerRankingData[], AllianceRankingData[], PlayerBrawlerRankingData[]>? _globalCache;

    private static readonly ConcurrentDictionary<string,
            Tuple<PlayerRankingData[], AllianceRankingData[], PlayerBrawlerRankingData[]>>
        RegionCaches = new();

    private readonly IDatabase _redisDb;
    private readonly List<string> _regionNames;

    public LeaderboardWorker(IConnectionMultiplexer redisConnection)
    {
        _redisDb = redisConnection.GetDatabase();

        var regions = LogicDataTables.GetAllDataByClassId<LogicRegionData>();

        if (regions == null)
            throw new Exception("Regions not found.");

        _regionNames = regions.Select(r => r.Name).Distinct().ToList();
    }

    public static int GlobalLeaderboardUpdateIntervalInMins { get; set; } = 1;
    public static int RegionsLeaderboardUpdateIntervalInMins { get; set; } = 1;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var globalTask = RunGlobalUpdatesAsync(stoppingToken);
        var regionalTask = RunRegionalUpdatesAsync(stoppingToken);

        await Task.WhenAll(globalTask, regionalTask);
    }

    private async Task RunGlobalUpdatesAsync(CancellationToken stoppingToken)
    {
        await UpdateGlobalCache(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(GlobalLeaderboardUpdateIntervalInMins), stoppingToken);
            await UpdateGlobalCache(stoppingToken);
        }
    }

    private async Task RunRegionalUpdatesAsync(CancellationToken stoppingToken)
    {
        await UpdateAllRegionsCache(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(RegionsLeaderboardUpdateIntervalInMins), stoppingToken);
            await UpdateAllRegionsCache(stoppingToken);
        }
    }

    private async Task UpdateGlobalCache(CancellationToken ct)
    {
        try
        {
            var playersTask = _redisDb.StringGetAsync("global_TopPlayers");
            var alliancesTask = _redisDb.StringGetAsync("global_TopAlliances");
            var brawlersTask = _redisDb.StringGetAsync("global_TopBrawlers");

            await Task.WhenAll(playersTask, alliancesTask, brawlersTask);

            var players = Deserialize<PlayerRankingData[]>(playersTask.Result);
            var alliances = Deserialize<AllianceRankingData[]>(alliancesTask.Result);
            var brawlers = Deserialize<PlayerBrawlerRankingData[]>(brawlersTask.Result);

            var newCache = Tuple.Create(
                players ?? [],
                alliances ?? [],
                brawlers ?? []
            );

            Interlocked.Exchange(ref _globalCache, newCache);

            Logger.Info(
                $"[Global leaderboard] Updated: P={players?.Length ?? 0}, A={alliances?.Length ?? 0}, B={brawlers?.Length ?? 0}");
        }
        catch (Exception ex)
        {
            Logger.Error($"[Global leaderboard] Error: {ex.Message}");
        }
    }

    private async Task UpdateAllRegionsCache(CancellationToken ct)
    {
        var tasks = _regionNames.Select(regionName => UpdateRegionCache(regionName, ct)).ToList();
        await Task.WhenAll(tasks);

        Logger.Info($"[Regional leaderboard] Updated {_regionNames.Count} regions");
    }

    private async Task UpdateRegionCache(string regionName, CancellationToken ct)
    {
        try
        {
            var playersTask = _redisDb.StringGetAsync($"region_{regionName}_TopPlayers");
            var alliancesTask = _redisDb.StringGetAsync($"region_{regionName}_TopAlliances");
            var brawlersTask = _redisDb.StringGetAsync($"region_{regionName}_TopBrawlers");

            await Task.WhenAll(playersTask, alliancesTask, brawlersTask);

            var players = Deserialize<PlayerRankingData[]>(playersTask.Result);
            var alliances = Deserialize<AllianceRankingData[]>(alliancesTask.Result);
            var brawlers = Deserialize<PlayerBrawlerRankingData[]>(brawlersTask.Result);

            var newCache = Tuple.Create(
                players ?? [],
                alliances ?? [],
                brawlers ?? []
            );

            RegionCaches.AddOrUpdate(regionName, newCache, (k, v) => newCache);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Regional/{regionName}] Error: {ex.Message}");
        }
    }

    private static T? Deserialize<T>(RedisValue value) where T : class
    {
        return value.IsNullOrEmpty ? null : MessagePackSerializer.Deserialize<T>(value!);
    }

    public static Tuple<PlayerRankingData[], AllianceRankingData[], PlayerBrawlerRankingData[]>? GetGlobalCache()
    {
        return Interlocked.CompareExchange(ref _globalCache, null, null);
    }

    public static Tuple<PlayerRankingData[], AllianceRankingData[], PlayerBrawlerRankingData[]>? GetRegionCache(
        string regionName)
    {
        return RegionCaches.TryGetValue(regionName, out var cache) ? cache : null;
    }
}