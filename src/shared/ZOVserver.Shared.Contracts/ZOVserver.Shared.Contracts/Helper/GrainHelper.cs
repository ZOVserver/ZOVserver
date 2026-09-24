using Orleans;
using ZOVserver.Shared.Contracts.Interfaces;

namespace ZOVserver.Shared.Contracts.Helper;

public static class GrainHelper
{
    public static IPlayerSessionServiceGrain GetPlayerSession(IGrainFactory factory, long id)
    {
        return factory.GetGrain<IPlayerSessionServiceGrain>($"g_player_session_{id}");
    }

    public static IHomeServiceGrain GetHomeGrain(IGrainFactory factory, long id)
    {
        return factory.GetGrain<IHomeServiceGrain>($"g_home_{id}");
    }

    public static IAllianceServiceGrain GetAllianceGrain(IGrainFactory factory, long id)
    {
        return factory.GetGrain<IAllianceServiceGrain>($"g_alliance_{id}");
    }

    public static ITeamServiceGrain GetTeamGrain(IGrainFactory factory, long id)
    {
        return factory.GetGrain<ITeamServiceGrain>($"g_team_{id}");
    }

    public static IFriendshipServiceGrain GetFriendshipGrain(IGrainFactory factory, long id)
    {
        return factory.GetGrain<IFriendshipServiceGrain>($"g_friendship_{id}");
    }

    public static IMatchmakingServiceGrain GetMatchmakingGrain(IGrainFactory factory, int slotId, int trophiesSector,
        string region, int difficulty = 0)
    {
        return factory.GetGrain<IMatchmakingServiceGrain>(
            $"g_matchmaking_{slotId}_{trophiesSector}_{region}_{difficulty}");
    }

    public static IBattleServiceGrain GetBattleGrain(IGrainFactory factory, Guid id)
    {
        return factory.GetGrain<IBattleServiceGrain>(id);
    }
}