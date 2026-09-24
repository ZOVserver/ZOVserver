namespace ZOVserver.Services.Shared.GarnetLeaderboardsService;

public class GarnetConfig
{
    public string? ConnectionString { get; set; }

    public bool AllowAdmin { get; set; } = true;

    public int ConnectRetry { get; set; } = 3;
    public bool AbortOnConnectFail { get; set; }

    public int MinRetryBackoff { get; set; } = 2000;
    public int MaxRetryBackoff { get; set; } = 15000;

    public int KeepAlive { get; set; } = 60;

    public string? Password { get; set; }
}