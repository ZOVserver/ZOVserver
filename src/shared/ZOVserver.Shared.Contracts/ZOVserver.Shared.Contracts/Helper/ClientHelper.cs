using Orleans;
using ZOVserver.Shared.Contracts.Interfaces;

namespace ZOVserver.Shared.Contracts.Helper;

public static class ClientHelper
{
    public static IClusterClient Client { get; set; } = null!;

    public static IPlayerSessionServiceGrain GetPlayerSession(long id)
    {
        return Client.GetGrain<IPlayerSessionServiceGrain>($"g_player_session_{id}");
    }

    public static IHomeServiceGrain GetHomeGrain(long id)
    {
        return Client.GetGrain<IHomeServiceGrain>($"g_home_{id}");
    }

    public static IAllianceServiceGrain GetAllianceGrain(long id)
    {
        return Client.GetGrain<IAllianceServiceGrain>($"g_alliance_{id}");
    }

    public static ITeamServiceGrain GeTeamGrain(long id)
    {
        return Client.GetGrain<ITeamServiceGrain>($"g_team_{id}");
    }

    public static IFriendshipServiceGrain GetFriendshipGrain(long id)
    {
        return Client.GetGrain<IFriendshipServiceGrain>($"g_friendship_{id}");
    }

    public static IMatchmakingServiceGrain GetMatchmakingGrain(int slotId, int trophiesSector, string region,
        int difficulty = 0)
    {
        return Client.GetGrain<IMatchmakingServiceGrain>(
            $"g_matchmaking_{slotId}_{trophiesSector}_{region}_{difficulty}");
    }

    public static IBattleServiceGrain GetBattleGrain(Guid id)
    {
        return Client.GetGrain<IBattleServiceGrain>(id);
    }
}