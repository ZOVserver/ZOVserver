using MessagePack;
using Microsoft.Extensions.Hosting;
using NLog;
using StackExchange.Redis;
using ZOVserver.Services.Global.LeaderboardEnrichmentService.GarnetLeaderboard;
using ZOVserver.Shared.Abstractions;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Interfaces;
using ZOVserver.Shared.Contracts.Models;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Services.Global.LeaderboardEnrichmentService.Worker;

public class RegionsLeaderboardEnrichmentWorker(IConnectionMultiplexer redisConnection) : BackgroundService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IDatabase _redisDb = redisConnection.GetDatabase();

    public static int RegionsLeaderboardRefreshIntervalInMins { get; set; } = 4;

    private static async Task<PlayerRankingData?> SafeGetRankingDataAsync(IHomeServiceGrain grain)
    {
        try
        {
            return await grain.GetMyRankingDataAsync();
        }
        catch
        {
            return null;
        }
    }

    private static async Task<PlayerBrawlerRankingData?> SafeGetBrawlerRankingDataAsync(IHomeServiceGrain grain,
        int brawlerId)
    {
        try
        {
            return await grain.GetMyBrawlerRankingDataAsync(brawlerId);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<AllianceRankingData?> SafeGetAllianceRankingDataAsync(IAllianceServiceGrain grain)
    {
        try
        {
            return await grain.GetAllianceRankingDataAsync();
        }
        catch
        {
            return null;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var brawlers = LogicDataTables.GetAllDataByClassId<LogicCharacterData>(16);

        if (brawlers == null)
            throw new Exception("Brawlers not found.");

        var regions = LogicDataTables.GetAllDataByClassId<LogicRegionData>();

        if (regions == null)
            throw new Exception("Regions not found.");

        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            Logger.Info("[Regions Enrichment Worker] Starting enrichment process.");

            var t = regions.Select(region => EnrichRegion(brawlers, region.Name, stoppingToken)).ToList();
            await Task.WhenAll(t);

            await Task.Delay(TimeSpan.FromMinutes(RegionsLeaderboardRefreshIntervalInMins), stoppingToken);
        }
    }

    private async Task EnrichRegion(LogicCharacterData[] brawlers, string region, CancellationToken stoppingToken)
    {
        try
        {
            var regionTopPlayers = await LeaderboardContainer.LeaderboardService.GetTopPlayersAsync(region);
            var regionTopAlliances = await LeaderboardContainer.LeaderboardService.GetTopAlliancesAsync(region);
            Dictionary<int, List<ILeaderboardEntry>> regionTopBrawlers = [];

            foreach (var brawler in brawlers)
            {
                var topBrawlers = await LeaderboardContainer.LeaderboardService
                    .GetTopBrawlersAsync(brawler.GlobalId, region);

                regionTopBrawlers.Add(brawler.GlobalId, topBrawlers);
            }

            var rt1 = regionTopPlayers
                .Select(player => ClientHelper.GetHomeGrain(player.Id))
                .Select(SafeGetRankingDataAsync).ToList();

            var rt2 = regionTopAlliances
                .Select(alliance => ClientHelper.GetAllianceGrain(alliance.Id))
                .Select(SafeGetAllianceRankingDataAsync).ToList();

            var rt3 = regionTopBrawlers
                .SelectMany(kvp => kvp.Value
                    .Select(entry => SafeGetBrawlerRankingDataAsync(
                        ClientHelper.GetHomeGrain(entry.Id),
                        kvp.Key)))
                .ToList();

            var rrt1 = (await Task.WhenAll(rt1)).Where(x => x != null)
                .OrderByDescending(x => x!.Trophies)
                .ToArray();
            var rrt2 = (await Task.WhenAll(rt2)).Where(x => x != null)
                .OrderByDescending(x => x!.Trophies)
                .ToArray();
            var rrt3 = (await Task.WhenAll(rt3)).Where(x => x != null)
                .OrderByDescending(x => x!.BrawlerTrophies)
                .ToArray();

            var saveTasks = new Task[]
            {
                _redisDb.StringSetAsync($"region_{region}_TopPlayers",
                    MessagePackSerializer.Serialize(rrt1, cancellationToken: stoppingToken)),
                _redisDb.StringSetAsync($"region_{region}_TopAlliances",
                    MessagePackSerializer.Serialize(rrt2, cancellationToken: stoppingToken)),
                _redisDb.StringSetAsync($"region_{region}_TopBrawlers",
                    MessagePackSerializer.Serialize(rrt3, cancellationToken: stoppingToken))
            };

            await Task.WhenAll(saveTasks);
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
            await Task.Delay(150, stoppingToken);
        }
    }
}