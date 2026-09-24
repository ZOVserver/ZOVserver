using NATS.Client.Core;
using ZOVserver.Shared.Contracts.Events;
using ZOVserver.Shared.Contracts.Events.Micro;

namespace ZOVserver.Bridge.TcpProtocolBridge.Manager;

public static class EventsManager
{
    private static int _id;
    private static IReadOnlyList<GameEventSlotEvent> _events = [];
    private static IReadOnlyList<GameEventSlotEvent> _upcomingEvents = [];
    private static int _doubleTokensEventFlag;
    private static int _shopClosedFlag;
    private static int _boxesClosedFlag;
    private static int _themeGlobalId;
    private static int _maintenanceSecondsLeft;

    public static async Task StartConsumerAsync(INatsConnection nats)
    {
        var slotsSub = nats.SubscribeAsync<GameEventSlotGlobalEvent>("GlobalEvents.GameSlots");

        _ = Task.Run(async () =>
        {
            await foreach (var msg in slotsSub)
            {
                var data = msg.Data;

                if (data == null)
                    continue;

                SetDoubleTokensEvent(data.DoubleTokensEvent);
                SetShopClosed(data.ShopClosed);
                SetBoxesClosed(data.BoxesClosed);
                SetThemeGlobalId(data.LobbyThemeGlobalId);
                SetMaintenanceSecondsLeft(data.MaintenanceSecondsLeft);

                if (GetId() == data.Id) continue;

                SetId(data.Id);
                SetEvents(data.Events.AsReadOnly());
                SetUpcomingEvents(data.UpcomingEvents.AsReadOnly());
            }
        });

        await Task.CompletedTask;
    }

    public static int GetId()
    {
        return Interlocked.CompareExchange(ref _id, 0, 0);
    }

    public static IReadOnlyList<GameEventSlotEvent> GetEvents()
    {
        return Interlocked.CompareExchange(ref _events!, null, null);
    }

    public static IReadOnlyList<GameEventSlotEvent> GetUpcomingEvents()
    {
        return Interlocked.CompareExchange(ref _upcomingEvents!, null, null);
    }

    public static bool GetDoubleTokensEvent()
    {
        return Interlocked.CompareExchange(ref _doubleTokensEventFlag, 0, 0) == 1;
    }

    public static bool GetShopClosed()
    {
        return Interlocked.CompareExchange(ref _shopClosedFlag, 0, 0) == 1;
    }

    public static bool GetBoxesClosed()
    {
        return Interlocked.CompareExchange(ref _boxesClosedFlag, 0, 0) == 1;
    }

    public static int GetThemeGlobalId()
    {
        return Interlocked.CompareExchange(ref _themeGlobalId, 0, 0);
    }

    public static int GetMaintenanceSecondsLeft()
    {
        return Interlocked.CompareExchange(ref _maintenanceSecondsLeft, 0, 0);
    }

    private static void SetId(int id)
    {
        Interlocked.Exchange(ref _id, id);
    }

    private static void SetEvents(IReadOnlyList<GameEventSlotEvent> newEvents)
    {
        Interlocked.Exchange(ref _events, newEvents);
    }

    private static void SetUpcomingEvents(IReadOnlyList<GameEventSlotEvent> newEvents)
    {
        Interlocked.Exchange(ref _upcomingEvents, newEvents);
    }

    private static void SetDoubleTokensEvent(bool value)
    {
        Interlocked.Exchange(ref _doubleTokensEventFlag, value ? 1 : 0);
    }

    private static void SetShopClosed(bool value)
    {
        Interlocked.Exchange(ref _shopClosedFlag, value ? 1 : 0);
    }

    private static void SetBoxesClosed(bool value)
    {
        Interlocked.Exchange(ref _boxesClosedFlag, value ? 1 : 0);
    }

    private static void SetThemeGlobalId(int value)
    {
        Interlocked.Exchange(ref _themeGlobalId, value);
    }

    private static void SetMaintenanceSecondsLeft(int value)
    {
        Interlocked.Exchange(ref _maintenanceSecondsLeft, value);
    }
}