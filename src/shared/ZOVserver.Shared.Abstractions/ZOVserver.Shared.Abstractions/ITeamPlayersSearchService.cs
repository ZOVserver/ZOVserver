namespace ZOVserver.Shared.Abstractions;

public interface ITeamPlayersSearchService
{
    public Task UpdateTeamInSearch(string bucket, string teamKey, double avgCups);
    public Task RemoveTeamFromSearch(string bucket, string teamKey);
    public Task<List<IPotentialTeam>> GetPotentialTeamsAsync(string bucket, int trophies, int radius, int count = 100);
}