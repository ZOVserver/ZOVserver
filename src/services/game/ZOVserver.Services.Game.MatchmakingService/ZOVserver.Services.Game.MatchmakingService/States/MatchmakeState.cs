namespace ZOVserver.Services.Game.MatchmakingService.States;

public class MatchmakeState
{
    public Guid Id { get; set; }
    public Dictionary<long, long> Players { get; set; } = [];
    public DateTime Created { get; set; } = DateTime.UtcNow;
}