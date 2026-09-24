using NLog;
using Orleans.Providers;
using ZLinq;
using ZOVserver.Services.Game.MatchmakingService.Manager;
using ZOVserver.Services.Game.MatchmakingService.States;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Interfaces;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;
using ZOVserver.Shared.TitanRemnants.Utility;

namespace ZOVserver.Services.Game.MatchmakingService.Grains;

[StorageProvider(ProviderName = "MemoryStorage")]
// ReSharper disable once UnusedType.Global
public class MatchmakingGrain : Grain<MatchmakingState>, IMatchmakingServiceGrain
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private IDisposable? _tickTimer;

    public static int MaxMatchmakingSeconds { get; set; }

    public async Task<(int, Guid)> AddPlayersToMatchmakingAsync(List<(long, long)> players, int avgTrophies,
        string region)
    {
        if (EventsManager.GetMaintenanceSecondsLeft() > 0)
            return (-5, Guid.Empty);

        if (!State.Region.Equals(region, StringComparison.CurrentCultureIgnoreCase))
            return (-1, Guid.Empty);

        var range = LogicGameModeUtil.GetTrophyRangeByMmSector(State.TrophiesSector);

        if (avgTrophies < range.min || avgTrophies > range.max)
            return (-2, Guid.Empty);

        if (players.Count == 0 || players.Count > State.MaxPlayersInTeam)
            return (-3, Guid.Empty);

        var teamId = players[0].Item2;

        if (players.AsValueEnumerable().Any(p => p.Item2 != teamId))
            return (-4, Guid.Empty);

        var capacities = LogicGamePlayUtil.GetTeamCapacitiesWithGameModeVariation(State.GameModeVariation,
            State.MaxPlayersInTeam, State.MaxPlayers);

        var ss = State.MatchmakeStates
            .AsValueEnumerable()
            .Where(s =>
            {
                if (s.Value.Players.Count + players.Count > State.MaxPlayers)
                    return false;

                var occupiedTeamSizes = s.Value.Players.Values
                    .AsValueEnumerable()
                    .Where(id => id > 0)
                    .GroupBy(id => id)
                    .Select(g => g.Count())
                    .OrderByDescending(c => c)
                    .ToList();

                for (var i = 0; i < capacities.Length; i++)
                {
                    var currentSize = i < occupiedTeamSizes.Count ? occupiedTeamSizes[i] : 0;

                    if (currentSize + players.Count <= capacities[i])
                        return true;
                }

                return false;
            }).ToArray();

        MatchmakeState selectedState;

        if (ss.Length > 0)
        {
            var randomIndex = Random.Shared.Next(ss.Length);

            selectedState = ss[randomIndex].Value;
        }
        else
        {
            selectedState = new MatchmakeState { Id = Guid.NewGuid() };

            State.MatchmakeStates.TryAdd(selectedState.Id, selectedState);
        }

        if (players.Any(p => selectedState.Players.ContainsKey(p.Item1)))
            return (-5, Guid.Empty);

        foreach (var (id, team) in players)
            selectedState.Players.Add(id, team);

        await SendMatchmakeStatusForAllPlayersAsync(selectedState.Id);

        return (0, selectedState.Id);
    }

    public async Task<int> RemovePlayersFromMatchmakingAsync(Guid id, List<long> players, bool force = false)
    {
        if (!State.MatchmakeStates.TryGetValue(id, out var value))
            return -1;

        if (value.Players.Count == State.MaxPlayers && !force)
            return -2;

        foreach (var p in players)
            value.Players.Remove(p);

        if (value.Players.Count == 0)
        {
            State.MatchmakeStates.Remove(id, out _);
            return 0;
        }

        await SendMatchmakeStatusForAllPlayersAsync(id);

        return 0;
    }

    public Task<int> IsContainsPlayersInMatchmakingAsync(Guid id, List<long> players)
    {
        if (!State.MatchmakeStates.TryGetValue(id, out var value))
            return Task.FromResult(-1);

        if (players.Any(p => !value.Players.ContainsKey(p)))
            return Task.FromResult(-2);

        return Task.FromResult(1);
    }

    private async Task SendMatchmakeStatusForAllPlayersAsync(Guid id)
    {
        if (!State.MatchmakeStates.TryGetValue(id, out var value))
            return;

        var s = (int)(value.Created + TimeSpan.FromSeconds(MaxMatchmakingSeconds) - DateTime.UtcNow).TotalSeconds;
        var fs = Math.Max(0, s);

        foreach (var p in value.Players)
            await GrainHelper.GetHomeGrain(GrainFactory, p.Key)
                .SendMatchmakeStatusAsync(value.Id, value.Players, State.MaxPlayers, fs);
    }

    private async Task KickFromMatchmakingForAllPlayersAsync(Guid id, int reason)
    {
        if (!State.MatchmakeStates.TryGetValue(id, out var value))
            return;

        foreach (var p in value.Players)
            await GrainHelper.GetHomeGrain(GrainFactory, p.Key)
                .KickFromMatchmakingAsync(value.Id, reason);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        State.SlotId = 0;
        State.TrophiesSector = 0;
        State.Region = string.Empty;
        State.MaxPlayers = 0;
        State.MaxPlayersInTeam = 0;
        State.LocationGlobalId = 0;
        State.GameModeVariation = 0;
        State.EventModifiers = [];

        _tickTimer?.Dispose();
        _tickTimer = null;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var data = this.GetPrimaryKeyString().Split('_');

        if (data.Length != 6)
            throw new Exception("Invalid key!");

        State.SlotId = Convert.ToInt32(data[2]);
        State.TrophiesSector = Convert.ToInt32(data[3]);
        State.Region = data[4].ToLower();
        State.Difficulty = Convert.ToInt32(data[5]);

        var events = EventsManager.GetEvents().ToArray();

        var eventData = events.FirstOrDefault(x => x.SlotId == State.SlotId);

        if (eventData == null)
            throw new Exception($"Invalid event {State.SlotId} in {string.Join(",", events.Select(x => x.SlotId))}!");

        var location = LogicDataTables.GetDataById<LogicLocationData>(eventData.LocationGlobalId);

        if (location == null)
            throw new Exception("Invalid location!");

        var gmodevar = LogicDataTables.GetDataByName<LogicGameModeVariationData>(
            GameModeToVariationConverter.GetGameModeVariation(location.GameMode));

        if (gmodevar == null)
            throw new Exception("Invalid game mode!");

        var maxPlayers = LogicGamePlayUtil.GetPlayerCountWithGameModeVariation(
            gmodevar.Variation, true);

        var maxPlayersInTeam = LogicGamePlayUtil.GetPlayerCountWithGameModeVariation(
            gmodevar.Variation, false);

        State.MaxPlayers = maxPlayers;
        State.MaxPlayersInTeam = maxPlayersInTeam;

        State.LocationGlobalId = location.GlobalId;
        State.GameModeVariation = gmodevar.Variation;
        State.EventModifiers = eventData.Modifiers.ToArray();

        _tickTimer ??= this.RegisterGrainTimer<object?>(
            async _ => await TickAsync(),
            null,
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromMilliseconds(500),
                Period = TimeSpan.FromMilliseconds(500),
                Interleave = false
            });
    }

    private async Task TickAsync()
    {
        var events = EventsManager.GetEvents().ToArray();

        var eventData = events.FirstOrDefault(x => x.SlotId == State.SlotId);

        if (eventData == null)
        {
            State.MaxPlayers = 0;
            return;
        }

        var location = LogicDataTables.GetDataById<LogicLocationData>(eventData.LocationGlobalId);

        if (location == null)
        {
            State.MaxPlayers = 0;
            return;
        }

        var gmodevar = LogicDataTables.GetDataByName<LogicGameModeVariationData>(
            GameModeToVariationConverter.GetGameModeVariation(location.GameMode));

        if (gmodevar == null)
        {
            State.MaxPlayers = 0;
            return;
        }

        var maxPlayers = LogicGamePlayUtil.GetPlayerCountWithGameModeVariation(
            gmodevar.Variation, true);

        if (State.LocationGlobalId != location.GlobalId || State.MaxPlayers != maxPlayers)
        {
            foreach (var s in State.MatchmakeStates)
                await KickFromMatchmakingForAllPlayersAsync(s.Key, 5);

            State.MatchmakeStates.Clear();
        }

        State.LocationGlobalId = location.GlobalId;
        State.MaxPlayers = maxPlayers;

        if (State.MatchmakeStates.IsEmpty)
            return;

        var readyRooms = State.MatchmakeStates
            .AsValueEnumerable()
            .Where(s => s.Value.Players.Count == State.MaxPlayers
                        || (MaxMatchmakingSeconds > 0 && s.Value.Created + TimeSpan.FromSeconds(MaxMatchmakingSeconds) <
                            DateTime.UtcNow))
            .ToList();

        if (readyRooms.Count == 0) return;

        var tasks = new List<Task>();

        foreach (var (id, room) in readyRooms)
        {
            State.MatchmakeStates.Remove(id, out _);
            tasks.Add(ToBattleServerAsync(State.LocationGlobalId, State.EventModifiers, State.Difficulty, room));
        }

        _ = Task.WhenAll(tasks);
    }

    private async Task ToBattleServerAsync(int locationGlobalId, int[] mods, int difficulty, MatchmakeState state)
    {
        var location = LogicDataTables.GetDataById<LogicLocationData>(locationGlobalId);

        if (location == null)
        {
            foreach (var p in state.Players)
                await GrainHelper.GetHomeGrain(GrainFactory, p.Key)
                    .KickFromMatchmakingAsync(state.Id, 5);

            return;
        }

        var gmodevar = LogicDataTables.GetDataByName<LogicGameModeVariationData>(
            GameModeToVariationConverter.GetGameModeVariation(location.GameMode));

        if (gmodevar == null)
        {
            foreach (var p in state.Players)
                await GrainHelper.GetHomeGrain(GrainFactory, p.Key)
                    .KickFromMatchmakingAsync(state.Id, 5);

            return;
        }

        var maxPlayers = LogicGamePlayUtil.GetPlayerCountWithGameModeVariation(
            gmodevar.Variation, true);

        if (state.Players.Count > maxPlayers)
        {
            foreach (var p in state.Players)
                await GrainHelper.GetHomeGrain(GrainFactory, p.Key)
                    .KickFromMatchmakingAsync(state.Id, 5);

            return;
        }

        foreach (var p in state.Players)
            await GrainHelper.GetHomeGrain(GrainFactory, p.Key).ToBattleFromMatchmakingAsync(state.Id);

        try
        {
            var b = GrainHelper.GetBattleGrain(GrainFactory, Guid.NewGuid());

            var res = await b.CreateBattleAsync(gmodevar.Variation, location.GlobalId, mods, difficulty,
                false,
                state.Players, []);

            if (res != 0)
            {
                Logger.Warn(
                    $"Error while creating battle: {res}! {location.GlobalId}_{string.Join(", ", mods)}_{difficulty}_{state.Players.Count}");

                foreach (var p in state.Players)
                    await GrainHelper.GetHomeGrain(GrainFactory, p.Key)
                        .KickFromMatchmakingAsync(state.Id, 15);
            }
        }
        catch (Exception e)
        {
            Logger.Warn(
                $"Error creating battle: {e.Message}! {location.GlobalId}_{string.Join(", ", mods)}_{difficulty}_{state.Players.Count}");

            foreach (var p in state.Players)
                await GrainHelper.GetHomeGrain(GrainFactory, p.Key)
                    .KickFromMatchmakingAsync(state.Id, 15);
        }
    }
}