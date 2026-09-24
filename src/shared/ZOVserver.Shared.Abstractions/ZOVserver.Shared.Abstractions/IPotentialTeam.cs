namespace ZOVserver.Shared.Abstractions;

public interface IPotentialTeam
{
    long TeamId { get; init; }
    int RegionId { get; init; }
    int AvgTrophies { get; init; }
    string? RawValue { get; init; }
}