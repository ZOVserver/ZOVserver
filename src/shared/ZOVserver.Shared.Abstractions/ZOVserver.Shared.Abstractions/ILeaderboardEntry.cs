namespace ZOVserver.Shared.Abstractions;

public interface ILeaderboardEntry
{
    public long Id { get; init; }
    public int Trophies { get; init; }
}