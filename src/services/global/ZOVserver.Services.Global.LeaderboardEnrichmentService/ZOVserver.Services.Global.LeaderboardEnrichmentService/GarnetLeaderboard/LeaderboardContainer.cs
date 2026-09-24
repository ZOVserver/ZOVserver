using ZOVserver.Shared.Abstractions;

namespace ZOVserver.Services.Global.LeaderboardEnrichmentService.GarnetLeaderboard;

public static class LeaderboardContainer
{
    public static ILeaderboardService LeaderboardService { get; set; } = null!;
}