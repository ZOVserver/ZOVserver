namespace ZOVserver.Services.Shared.AllianceSearchService;

public class PoolSettings
{
    public required Uri[] Nodes { get; set; }
    public required ConnectionPoolType PoolType { get; set; }
}