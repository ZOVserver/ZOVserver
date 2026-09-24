using ZOVserver.Shared.Abstractions;

namespace ZOVserver.Services.Shared.GarnetLeaderboardsService;

public record LeaderboardEntry : ILeaderboardEntry
{
    public long Id { get; init; }
    public int Trophies { get; init; }
}