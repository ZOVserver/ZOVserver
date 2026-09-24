using Grpc.Core;
using Statisticsservice;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.TitanRemnants.Mathem.Shared;
using Empty = Statisticsservice.Empty;

namespace ZOVserver.AdminPanel.Backend.Services;

public class StatisticsService(ILogger<StatisticsService> logger, MetricsAggregatorService metricsAggregator)
    : Statisticsservice.StatisticsService.StatisticsServiceBase
{
    public override async Task<GlobalStatsResponse> GetGlobalStats(Empty request, ServerCallContext context)
    {
        var activePlayers = await metricsAggregator.GetSum("players_online_count", "tenant_fast");
        var activeBattles = await metricsAggregator.GetSum("active_battles_count", "tenant_fast");

        var plrsc = await metricsAggregator.GetSum("player_registrations_total", "tenant_long");
        var allcc = await metricsAggregator.GetSum("club_registrations_total", "tenant_long");
        var gamrc = await metricsAggregator.GetSum("room_registrations_total", "tenant_long");

        logger.LogInformation("Stats: Online: {act}, Total Players: {plrs}", activePlayers, plrsc);

        return new GlobalStatsResponse
        {
            Success = true,
            ActivePlayers = activePlayers,
            Players = plrsc,
            Alliances = allcc,
            GameRooms = gamrc,
            ActiveBattles = activeBattles
        };
    }

    public override async Task<PlayerStatsResponse> GetPlayerStats(PlayerStatsRequest request,
        ServerCallContext context)
    {
        var playerId = new LogicLong(request.HighLowId.High, request.HighLowId.Low);
        var playerSession = ClientHelper.GetPlayerSession(playerId);

        logger.LogInformation("Getting player stats for: {user}.", request.UserId);

        var ban = await playerSession.GetBanInfo();
        var locked = await playerSession.GetLockInfo();

        var response = new PlayerStatsResponse
        {
            Success = true,
            Message = "Player statistics retrieved successfully",

            IsBanned = ban.Item1,
            BanReason = ban.Item3,
            BanEndTime = ban.Item1 ? ban.Item2.ToString("yyyy-MM-dd HH:mm:ss") : "",

            IsLocked = locked.Item1,
            UnlockCode = locked.Item1 ? locked.Item2 : "",

            SessionsCount = await playerSession.GetSessionsCount(),
            CreationTime = (await playerSession.GetAccountCreatedTime()).ToString("yyyy-MM-dd HH:mm:ss"),
            PlayTimeSeconds = (long)await playerSession.GetPlayTimeSeconds(),
            DeviceLanguage = await playerSession.GetPlayerDeviceLanguage()
        };

        var devices = await playerSession.GetDevicesInfo();
        foreach (var device in devices)
            response.Devices.Add(device.Key, device.Value);

        var servers = await playerSession.GetServersInfo();
        foreach (var server in servers)
            response.ServersConnected.Add(server.Key, server.Value);

        var clients = await playerSession.GetClientsInfo();
        foreach (var client in clients)
            response.ClientIps.Add(client.Key, client.Value);

        return response;
    }
}