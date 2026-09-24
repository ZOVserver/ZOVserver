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

public class GlobalLeaderboardEnrichmentWorker(IConnectionMultiplexer redisConnection) : BackgroundService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IDatabase _redisDb = redisConnection.GetDatabase();

    public static int GlobalLeaderboardRefreshIntervalInMins { get; set; } = 3;

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

        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            Logger.Info("[Global Enrichment Worker] Starting enrichment process.");

            try
            {
                var globalTopPlayers = await LeaderboardContainer.LeaderboardService.GetTopPlayersAsync(count: 200);
                var globalTopAlliances = await LeaderboardContainer.LeaderboardService.GetTopAlliancesAsync(count: 200);

                Dictionary<int, List<ILeaderboardEntry>> globalTopBrawlers = [];

                foreach (var brawler in brawlers)
                {
                    var topBrawlers = await LeaderboardContainer.LeaderboardService
                        .GetTopBrawlersAsync(brawler.GlobalId, count: 200);

                    globalTopBrawlers.Add(brawler.GlobalId, topBrawlers);
                }

                var gt1 = globalTopPlayers
                    .Select(player => ClientHelper.GetHomeGrain(player.Id))
                    .Select(SafeGetRankingDataAsync).ToList();

                var gt2 = globalTopAlliances
                    .Select(alliance => ClientHelper.GetAllianceGrain(alliance.Id))
                    .Select(SafeGetAllianceRankingDataAsync).ToList();

                var gt3 = globalTopBrawlers
                    .SelectMany(kvp => kvp.Value
                        .Select(entry => SafeGetBrawlerRankingDataAsync(
                            ClientHelper.GetHomeGrain(entry.Id),
                            kvp.Key)))
                    .ToList();

                var rgt1 = (await Task.WhenAll(gt1)).Where(x => x != null)
                    .OrderByDescending(x => x!.Trophies)
                    .ToArray();
                var rgt2 = (await Task.WhenAll(gt2)).Where(x => x != null)
                    .OrderByDescending(x => x!.Trophies)
                    .ToArray();
                var rgt3 = (await Task.WhenAll(gt3)).Where(x => x != null)
                    .OrderByDescending(x => x!.BrawlerTrophies)
                    .ToArray();

                var saveTasks = new Task[]
                {
                    _redisDb.StringSetAsync("global_TopPlayers",
                        MessagePackSerializer.Serialize(rgt1, cancellationToken: stoppingToken)),
                    _redisDb.StringSetAsync("global_TopAlliances",
                        MessagePackSerializer.Serialize(rgt2, cancellationToken: stoppingToken)),
                    _redisDb.StringSetAsync("global_TopBrawlers",
                        MessagePackSerializer.Serialize(rgt3, cancellationToken: stoppingToken))
                };

                await Task.WhenAll(saveTasks);
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
                await Task.Delay(150, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromMinutes(GlobalLeaderboardRefreshIntervalInMins), stoppingToken);
        }
    }
}