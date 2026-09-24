namespace ZOVserver.Services.Shared.AllianceSearchService;

public class ConnSettings
{
    public required string Index { get; set; } = string.Empty;

    public TimeSpan PingTimeout { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(60);

    public int MaxRetries { get; set; } = 2;
    public TimeSpan RetryTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public bool UsePrettyJson { get; set; }
    public bool EnableDebugMode { get; set; }

    public bool EnableHttpCompression { get; set; }
    public bool EnableHttpPipelining { get; set; }

    public (string, string)? BasicAuthentication { get; set; }
    public (string, string)? ApiKeyAuthentication { get; set; }
}