namespace ZOVserver.Shared.Contracts.Events.Micro;

public class GameEventSlotEvent
{
    public int EventId { get; set; }

    public int SlotId { get; set; }
    public int LocationGlobalId { get; set; }
    public List<int> Modifiers { get; set; } = [];

    public int MiniBoxTokensReward { get; set; }

    public DateTime StartOrEndTime { get; set; }
}