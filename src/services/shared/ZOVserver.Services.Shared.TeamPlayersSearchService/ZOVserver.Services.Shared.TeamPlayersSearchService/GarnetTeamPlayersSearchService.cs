using StackExchange.Redis;
using ZOVserver.Shared.Abstractions;

namespace ZOVserver.Services.Shared.TeamPlayersSearchService;

public class GarnetTeamPlayersSearchService : ITeamPlayersSearchService
{
    private readonly IDatabase _db;

    public GarnetTeamPlayersSearchService(GarnetConfig config)
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

    public async Task UpdateTeamInSearch(string bucket, string teamKey, double avgCups)
    {
        await _db.SortedSetAddAsync(bucket, teamKey, avgCups);
    }

    public async Task RemoveTeamFromSearch(string bucket, string teamKey)
    {
        await _db.SortedSetRemoveAsync(bucket, teamKey);
    }

    public async Task<List<IPotentialTeam>> GetPotentialTeamsAsync(string bucket, int trophies, int radius,
        int count = 100)
    {
        var entries = await _db.SortedSetRangeByScoreWithScoresAsync(
            bucket,
            trophies - radius,
            trophies + radius,
            take: count);

        if (entries.Length == 0)
            return [];

        var result = new List<IPotentialTeam>(entries.Length);

        foreach (var entry in entries)
        {
            var raw = entry.Element.ToString();
            var span = raw.AsSpan();
            var splitIdx = span.IndexOf(':');

            if (splitIdx == -1) continue;

            if (int.TryParse(span[..splitIdx], out var regionId) &&
                long.TryParse(span[(splitIdx + 1)..], out var teamId))
                result.Add(new PotentialTeam
                {
                    TeamId = teamId,
                    RegionId = regionId,
                    AvgTrophies = (int)entry.Score,
                    RawValue = raw
                });
        }

        return result;
    }
}