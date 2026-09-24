using Prometheus;

namespace ZOVserver.Services.Game.HomeService.Telemetry;

public static class MetricsClient
{
    // Fast metrics (15-day retention)


    // Long-term metrics (100-year retention)
    private static readonly Counter ClubRegistrationsCounter = Metrics
        .CreateCounter("club_registrations_total", "Cumulative count of club registrations");

    private static readonly Counter RoomRegistrationsCounter = Metrics
        .CreateCounter("room_registrations_total", "Cumulative count of game room registrations");

    public static void ObserveClubRegistration()
    {
        ClubRegistrationsCounter.Inc();
    }

    public static void ObserveRoomRegistration()
    {
        RoomRegistrationsCounter.Inc();
    }
}