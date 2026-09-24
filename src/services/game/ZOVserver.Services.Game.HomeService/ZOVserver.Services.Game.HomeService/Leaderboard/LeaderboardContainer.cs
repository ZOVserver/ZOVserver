using ZOVserver.Shared.Abstractions;

namespace ZOVserver.Services.Game.HomeService.Leaderboard;

public static class LeaderboardContainer
{
    public static ILeaderboardService LeaderboardService { get; set; } = null!;
}