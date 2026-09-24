namespace ZOVserver.Shared.Abstractions;

public interface ILeaderboardService
{
    public Task UpdatePlayerFullDataAsync(long accountId, string? oldRegion, string? newRegion,
        Dictionary<int, int> brawlerTrophies);

    public Task UpdateAllianceRegionAsync(long allianceId, string? oldRegion, string? newRegion, int trophies);

    public Task UpdatePlayerTrophiesAsync(long accountId, int totalTrophies, int brawlerId, int brawlerTrophies,
        string? region);

    public Task UpdateAllianceTrophiesAsync(long allianceId, int trophies, string? region);

    public Task<List<ILeaderboardEntry>> GetTopPlayersAsync(string? region = null, int count = 200);
    public Task<List<ILeaderboardEntry>> GetTopBrawlersAsync(int brawlerId, string? region = null, int count = 200);
    public Task<List<ILeaderboardEntry>> GetTopAlliancesAsync(string? region = null, int count = 200);
}