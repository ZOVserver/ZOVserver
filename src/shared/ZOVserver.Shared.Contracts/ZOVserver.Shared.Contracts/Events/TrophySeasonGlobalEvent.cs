namespace ZOVserver.Shared.Contracts.Events;

public class TrophySeasonGlobalEvent
{
    public int SeasonCounter { get; set; }
    public DateTime SeasonEndTime { get; set; }
    public int SeasonDurationMinutes { get; set; }
}