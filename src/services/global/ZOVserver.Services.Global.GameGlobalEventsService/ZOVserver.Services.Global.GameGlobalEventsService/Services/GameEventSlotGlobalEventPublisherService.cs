using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using ZLinq;
using ZOVserver.Services.Global.GameGlobalEventsService.Configs.Models;
using ZOVserver.Services.Global.GameGlobalEventsService.FileProvider;
using ZOVserver.Services.Global.GameGlobalEventsService.Grpc;
using ZOVserver.Services.Global.GameGlobalEventsService.Settings;
using ZOVserver.Shared.Contracts.Events;
using ZOVserver.Shared.Contracts.Events.Micro;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Services.Global.GameGlobalEventsService.Services;

public class GameEventSlotGlobalEventPublisherService(
    INatsConnection nats,
    ILogger<GameEventSlotGlobalEventPublisherService> logger) : BackgroundService
{
    private readonly List<GameEventSlotEvent> _activeEvents = [];

    private readonly ConcurrentDictionary<int, int> _customEvents = new();

    private readonly ThreadSafeFileProvider<Events> _events = new(Path.GetFullPath(@"Configs\events.yml"));

    private readonly List<GameEventSlotEvent> _upcomingEvents = [];

    private int _lastId;

    public bool DoubleTokensEvent { get; set; }

    public bool ShopClosed { get; set; }

    public bool BoxesClosed { get; set; }

    public int ThemeGlobalId { get; set; } = HomeSettings.GetConfig().Decorations.DefaultLobbyThemeGid;

    public int MaintenanceSecondsLeft { get; set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        GameGlobalEventsGrpcService.EventService = this;

        if (_events.GetData().GenerateGemGrab)
            GenerateGemGrab();

        if (_events.GetData().GenerateShowdown)
            GenerateShowdown();

        if (_events.GetData().GenerateDailyEvents)
            GenerateDailyEvents();

        if (_events.GetData().GenerateSpecialEvents)
            GenerateSpecialEvents();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updated = false;

                var sg = false;
                foreach (var e in _upcomingEvents.ToArray())
                {
                    if (DateTime.UtcNow < e.StartOrEndTime) continue;

                    updated = true;

                    _activeEvents.RemoveAll(x => x.SlotId == e.SlotId);

                    if (_customEvents.TryGetValue(e.EventId, out var seconds))
                        e.StartOrEndTime += TimeSpan.FromSeconds(seconds);
                    else
                        e.StartOrEndTime += GetEventLifetime(e.SlotId);

                    _activeEvents.Add(e);

                    switch (e.SlotId)
                    {
                        case 1:
                            GenerateGemGrab();
                            break;
                        case 3:
                            GenerateDailyEvents();
                            break;
                        case 4:
                            GenerateSpecialEvents();
                            break;
                        case 2 or 5 when sg:
                            continue;
                        case 2 or 5:
                            GenerateShowdown();
                            sg = true;
                            break;
                        default:
                            _upcomingEvents.RemoveAll(x => x.SlotId == e.SlotId);
                            break;
                    }
                }

                foreach (var e in _activeEvents.ToArray())
                {
                    if (DateTime.UtcNow < e.StartOrEndTime) continue;

                    updated = true;

                    _activeEvents.RemoveAll(x => x.SlotId == e.SlotId);
                }

                if (updated)
                    _lastId = Random.Shared.Next(0, int.MaxValue);

                var ge = new GameEventSlotGlobalEvent
                {
                    Id = _lastId,
                    Events = _activeEvents, UpcomingEvents = _upcomingEvents,
                    DoubleTokensEvent = DoubleTokensEvent, ShopClosed = ShopClosed, BoxesClosed = BoxesClosed,
                    LobbyThemeGlobalId = ThemeGlobalId, MaintenanceSecondsLeft = MaintenanceSecondsLeft
                };

                await nats.PublishAsync("GlobalEvents.GameSlots", ge, cancellationToken: stoppingToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            await Task.Delay(250, stoppingToken);
        }
    }

    public int AddNewUpcomingEvent(int slot, int locationId, List<int> modifiers, int miniBoxTokensReward,
        long startTime, int secondsLifetime)
    {
        if (slot is 1 or 2 or 3 or 4 or 5)
            return -1000;

        if (_upcomingEvents.Any(x => x.SlotId == slot))
            return -1001;

        if (_activeEvents.Any(x => x.SlotId == slot))
            return -1002;

        var id = Random.Shared.Next(0, int.MaxValue);

        if (!_customEvents.TryAdd(id, secondsLifetime))
            return -1003;

        if (modifiers.Count > 5)
            return -1004;

        if (locationId < 15_000_000)
            return -1005;

        if (LogicDataTables.GetDataById(locationId) == null)
            return -1005;

        _upcomingEvents.Add(new GameEventSlotEvent
        {
            EventId = id,
            SlotId = slot,
            LocationGlobalId = locationId,
            StartOrEndTime = DateTimeOffset.FromUnixTimeSeconds(startTime).DateTime,
            MiniBoxTokensReward = miniBoxTokensReward,
            Modifiers = modifiers
        });

        return id;
    }

    public int RemoveUpcomingEvent(int id)
    {
        var r1 = _customEvents.TryRemove(id, out _);
        var r2 = _upcomingEvents.RemoveAll(x => x.EventId == id);


        if (!r1)
            return -2005;

        if (r2 < 1)
            return -2006;

        return 1;
    }

    public GameEventSlotEvent[] GetUpcomingEvents()
    {
        return _upcomingEvents.ToArray();
    }

    private void GenerateSpecialEvents()
    {
        var now = DateTime.UtcNow;
        var isFirst = !_upcomingEvents.Any(x => x.SlotId is 4);

        var allGameModes = _events.GetData().SpecialEventsGameModes.ToList();
        var shuffledGameModes = allGameModes.OrderBy(_ => Random.Shared.Next()).ToList();

        LogicLocationData? randomLocation = null;

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var gameMode in shuffledGameModes)
        {
            var locations = LogicDataTables.GetAllDataByClassId<LogicLocationData>()?
                .AsValueEnumerable()
                .Where(x => !x.Disabled)
                .Where(x => x.GameMode == gameMode)
                .ToArray();

            if (locations is not { Length: > 0 }) continue;

            randomLocation = locations[Random.Shared.Next(locations.Length)];
            break;
        }

        if (randomLocation == null)
            throw new InvalidOperationException(
                $"No locations available for any game mode. Checked modes: {string.Join(", ", allGameModes)}");

        _upcomingEvents.RemoveAll(x => x.SlotId is 4);

        _upcomingEvents.Add(new GameEventSlotEvent
        {
            EventId = Random.Shared.Next(0, int.MaxValue),
            SlotId = 4,
            LocationGlobalId = randomLocation.GlobalId,
            StartOrEndTime = isFirst ? now : now + GetEventLifetime(4),
            MiniBoxTokensReward = 10,
            Modifiers = GenerateModifiers(4)
        });
    }

    private void GenerateDailyEvents()
    {
        var now = DateTime.UtcNow;
        var isFirst = !_upcomingEvents.Any(x => x.SlotId is 3);

        var allGameModes = _events.GetData().DailyEventsGameModes.ToList();
        var shuffledGameModes = allGameModes.OrderBy(_ => Random.Shared.Next()).ToList();

        LogicLocationData? randomLocation = null;

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var gameMode in shuffledGameModes)
        {
            var locations = LogicDataTables.GetAllDataByClassId<LogicLocationData>()?
                .AsValueEnumerable()
                .Where(x => !x.Disabled)
                .Where(x => x.GameMode == gameMode)
                .ToArray();

            if (locations is not { Length: > 0 })
            {
                logger.LogWarning("No locations available for game mode: {gm}", gameMode);
                continue;
            }

            randomLocation = locations[Random.Shared.Next(locations.Length)];
            break;
        }

        if (randomLocation == null)
            throw new InvalidOperationException(
                $"No locations available for any game mode. Checked modes: {string.Join(", ", allGameModes)}");

        _upcomingEvents.RemoveAll(x => x.SlotId is 3);

        _upcomingEvents.Add(new GameEventSlotEvent
        {
            EventId = Random.Shared.Next(0, int.MaxValue),
            SlotId = 3,
            LocationGlobalId = randomLocation.GlobalId,
            StartOrEndTime = isFirst ? now : now + GetEventLifetime(3),
            MiniBoxTokensReward = 10,
            Modifiers = GenerateModifiers(3)
        });
    }

    private void GenerateGemGrab()
    {
        var now = DateTime.UtcNow;
        var isFirst = !_upcomingEvents.Any(x => x.SlotId is 1);

        var locations = LogicDataTables.GetAllDataByClassId<LogicLocationData>()?
            .AsValueEnumerable()
            .Where(x => !x.Disabled)
            .Where(x => x.GameMode == "CoinRush")
            .ToArray();

        if (locations == null || locations.Length == 0)
            throw new Exception("No locations available for game mode CoinRush");

        var randomLocation = locations[Random.Shared.Next(locations.Length)];

        _upcomingEvents.RemoveAll(x => x.SlotId is 1);

        _upcomingEvents.Add(new GameEventSlotEvent
        {
            EventId = Random.Shared.Next(0, int.MaxValue),
            SlotId = 1,
            LocationGlobalId = randomLocation.GlobalId,
            StartOrEndTime = isFirst ? now : now + GetEventLifetime(1),
            MiniBoxTokensReward = 10,
            Modifiers = GenerateModifiers(1)
        });
    }

    private void GenerateShowdown()
    {
        var now = DateTime.UtcNow;
        var isFirst = !_upcomingEvents.Any(x => x.SlotId is 2 or 5);

        var locations = LogicDataTables.GetAllDataByClassId<LogicLocationData>()?
            .AsValueEnumerable()
            .Where(x => !x.Disabled)
            .Where(x => x.GameMode == "BattleRoyale")
            .ToArray();

        if (locations == null || locations.Length == 0)
            throw new Exception("No locations available for game mode BattleRoyale");

        var randomSoloLocation = locations[Random.Shared.Next(locations.Length)];

        var randomDuoLocation = LogicDataTables.GetAllDataByClassId<LogicLocationData>()?
            .AsValueEnumerable()
            .Where(x => !x.Disabled)
            .FirstOrDefault(x =>
                x.AllowedMaps == randomSoloLocation.AllowedMaps &&
                x.GlobalId != randomSoloLocation.GlobalId);

        if (randomDuoLocation == null)
            throw new Exception($"Duo location is null: {randomSoloLocation.AllowedMaps}.");

        _upcomingEvents.RemoveAll(x => x.SlotId is 2 or 5);

        var mods = GenerateModifiers(2);

        _upcomingEvents.Add(new GameEventSlotEvent
        {
            EventId = Random.Shared.Next(0, int.MaxValue),
            SlotId = 2,
            LocationGlobalId = randomSoloLocation.GlobalId,
            StartOrEndTime = isFirst ? now : now + GetEventLifetime(2),
            MiniBoxTokensReward = 10,
            Modifiers = mods
        });

        _upcomingEvents.Add(new GameEventSlotEvent
        {
            EventId = Random.Shared.Next(0, int.MaxValue),
            SlotId = 5,
            LocationGlobalId = randomDuoLocation.GlobalId,
            StartOrEndTime = isFirst ? now : now + GetEventLifetime(5),
            MiniBoxTokensReward = 10,
            Modifiers = mods
        });
    }

    private static TimeSpan GetEventLifetime(int slot)
    {
        return slot switch
        {
            1 => TimeSpan.FromHours(24),
            2 or 5 => TimeSpan.FromHours(12),
            3 => TimeSpan.FromHours(16),
            4 => TimeSpan.FromHours(16),
            _ => TimeSpan.FromHours(18)
        };
    }

    private static List<int> GenerateModifiers(int slot)
    {
        if (slot != 2)
            return [];

        var mods = new List<int>();

        if (Random.Shared.Next(100) >= 15)
            return mods;

        var mod1 = Random.Shared.Next(1, 6);
        mods.Add(mod1);

        if (Random.Shared.Next(100) >= 2) return mods;

        var mod2 = Random.Shared.Next(1, 6);
        while (mods.Contains(mod2))
            mod2 = Random.Shared.Next(1, 6);

        mods.Add(mod2);

        return mods;
    }
}