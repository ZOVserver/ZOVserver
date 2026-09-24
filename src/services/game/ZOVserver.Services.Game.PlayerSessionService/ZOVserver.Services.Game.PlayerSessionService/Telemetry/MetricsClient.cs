using Prometheus;

namespace ZOVserver.Services.Game.PlayerSessionService.Telemetry;

public static class MetricsClient
{
    // Fast metrics (15-day retention)
    private static readonly Gauge OnlinePlayersGauge = Metrics
        .CreateGauge("players_online_count", "Current number of online players in this silo");

    // Long-term metrics (100-year retention)
    private static readonly Counter PlayerRegistrationsCounter = Metrics
        .CreateCounter("player_registrations_total", "Cumulative count of player registrations");

    public static void ObservePlayerConnected()
    {
        OnlinePlayersGauge.Inc();
    }

    public static void ObservePlayerDisconnected()
    {
        if (OnlinePlayersGauge.Value <= 0)
            OnlinePlayersGauge.Set(0);
        else
            OnlinePlayersGauge.Dec();
    }

    public static void ObservePlayerRegistration()
    {
        PlayerRegistrationsCounter.Inc();
    }
}