using Orleans;
using Orleans.Concurrency;

namespace ZOVserver.Shared.Contracts.Interfaces;

[Alias("ZOVserver.Shared.Contracts.Interfaces.IMatchmakingServiceGrain")]
public interface IMatchmakingServiceGrain : IGrainWithStringKey
{
    [Alias("AddPlayersToMatchmaking")]
    public Task<(int, Guid)> AddPlayersToMatchmakingAsync(List<(long, long)> players, int avgTrophies, string region);

    [Alias("RemovePlayersFromMatchmaking")]
    public Task<int> RemovePlayersFromMatchmakingAsync(Guid id, List<long> players, bool force = false);

    [Alias("IsContainsPlayersInMatchmaking")]
    [ReadOnly]
    public Task<int> IsContainsPlayersInMatchmakingAsync(Guid id, List<long> players);
}