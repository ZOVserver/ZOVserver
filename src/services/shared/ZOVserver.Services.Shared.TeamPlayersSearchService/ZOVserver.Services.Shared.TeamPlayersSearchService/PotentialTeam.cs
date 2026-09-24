using ZOVserver.Shared.Abstractions;

namespace ZOVserver.Services.Shared.TeamPlayersSearchService;

public record PotentialTeam : IPotentialTeam
{
    public long TeamId { get; init; }
    public int RegionId { get; init; }
    public int AvgTrophies { get; init; }
    public string? RawValue { get; init; }
}