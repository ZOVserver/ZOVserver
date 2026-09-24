using ZOVserver.Shared.Contracts.Events.Micro;

namespace ZOVserver.Shared.Contracts.Events;

public class GameEventSlotGlobalEvent
{
    public int Id { get; set; }

    public List<GameEventSlotEvent> Events { get; set; } = [];
    public List<GameEventSlotEvent> UpcomingEvents { get; set; } = [];

    public bool DoubleTokensEvent { get; set; }
    public bool ShopClosed { get; set; }
    public bool BoxesClosed { get; set; }

    public int LobbyThemeGlobalId { get; set; }

    public int MaintenanceSecondsLeft { get; set; }
}