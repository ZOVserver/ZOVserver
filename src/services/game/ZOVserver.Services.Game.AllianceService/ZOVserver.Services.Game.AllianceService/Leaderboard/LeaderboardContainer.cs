using ZOVserver.Shared.Abstractions;

namespace ZOVserver.Services.Game.AllianceService.Leaderboard;

public static class LeaderboardContainer
{
    public static ILeaderboardService LeaderboardService { get; set; } = null!;
}