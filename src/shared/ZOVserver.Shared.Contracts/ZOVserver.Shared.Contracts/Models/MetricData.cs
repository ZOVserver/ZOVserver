using MessagePack;

namespace ZOVserver.Shared.Contracts.Models;

[MessagePackObject]
public class MetricData
{
    [Key(0)] public string Name { get; set; } = string.Empty;

    [Key(1)] public int Value { get; set; }
}