using System.Buffers.Binary;
using System.Security.Cryptography;
using Cysharp.Threading;
using NLog;
using Orleans.Providers;
using ZOVserver.RasputinProtect;
using ZOVserver.Services.Game.BattleService.Game;
using ZOVserver.Services.Game.BattleService.Network;
using ZOVserver.Services.Game.BattleService.States;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Interfaces;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;
using ZOVserver.Shared.Contracts.Laser.Machine;
using ZOVserver.Shared.Contracts.Laser.Messages.Server;
using ZOVserver.Shared.Contracts.Structs;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;
using ZOVserver.Shared.TitanRemnants.Utility;

namespace ZOVserver.Services.Game.BattleService.Grains;

[StorageProvider(ProviderName = "MemoryStorage")]
// ReSharper disable once UnusedType.Global
public class BattleGrain(ILogicLooperPool looperPool) : Grain<BattleState>, IBattleServiceGrain
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private BattleState? _dataRef;

    public async Task<int> CreateBattleAsync(int gameModeVar, int locationGlobalId, int[] eventModifiers,
        int difficulty, bool isFriendlyGame,
        Dictionary<long, long> players, Dictionary<long, int> playersFriendlyTeam)
    {
        try
        {
            if (State.BattleCreated)
                return -1;

            var location = LogicDataTables.GetDataById<LogicLocationData>(locationGlobalId);

            if (location == null)
                return -2;

            var gmodevar = LogicDataTables.GetDataByName<LogicGameModeVariationData>(
                GameModeToVariationConverter.GetGameModeVariation(location.GameMode));

            if (gmodevar == null)
                return -3;

            if (gmodevar.Variation != gameModeVar)
                return -4;

            var maxPlayers = LogicGamePlayUtil.GetPlayerCountWithGameModeVariation(
                gmodevar.Variation, true);

            if (players.Count > maxPlayers)
                return -5;

            var rooms = players
                .Where(x => x.Value > 0)
                .GroupBy(pair => pair.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(pair => pair.Key).ToList()
                );

            var friendlyTeams = playersFriendlyTeam
                .GroupBy(pair => pair.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(pair => pair.Key).ToList()
                );

            var maxPlayersInTeam = LogicGamePlayUtil.GetPlayerCountWithGameModeVariation(
                gmodevar.Variation, false);

            if (!isFriendlyGame)
            {
                if (rooms.Any(r => r.Value.Count > maxPlayersInTeam))
                    return -6;
            }
            else
            {
                if (friendlyTeams.Any(r => r.Value.Count > maxPlayersInTeam))
                    return -7;
            }

            if (eventModifiers.Any(m => m is not (1 or 2 or 3 or 4 or 5 or 8 or 9 or 10 or 11)))
                return -8;

            if (difficulty is < 0 or > 16)
                return -9;

            State.BattleServer = BattleServerManager.GetBestInstance();

            if (State.BattleServer == null)
                return -10;

            State.GameMode = gameModeVar;
            State.LocationGlobalId = locationGlobalId;
            State.PlayersTeam = players;
            State.PlayersFriendlyTeam = playersFriendlyTeam;
            State.IsFriendlyBattle = isFriendlyGame;
            State.Rooms = rooms;
            State.FriendlyTeams = friendlyTeams;
            State.EventModifiers = eventModifiers;
            State.Difficulty = difficulty;
            State.BattleCreated = true;

            var t = players.Select(p => GetPlayer(p.Key)).ToList();
            var res = await Task.WhenAll(t);

            var logicPlayers = res.OfType<LogicPlayer>().ToList();
            var activeIds = logicPlayers.Select(x => x.AccountId).ToList();

            State.PlayersTeam = State.PlayersTeam
                .Where(x => activeIds.Contains(x.Key))
                .ToDictionary();

            State.PlayersFriendlyTeam = State.PlayersFriendlyTeam
                .Where(x => activeIds.Contains(x.Key))
                .ToDictionary();

            foreach (var room in State.Rooms.Values)
                room.RemoveAll(id => !activeIds.Contains(id));

            foreach (var room in State.FriendlyTeams.Values)
                room.RemoveAll(id => !activeIds.Contains(id));

            var capacities =
                LogicGamePlayUtil.GetTeamCapacitiesWithGameModeVariation(gameModeVar, maxPlayersInTeam,
                    maxPlayers);

            var friendlyTeamCount = new Dictionary<int, int>();
            var i = 0;

            foreach (var p in logicPlayers)
            {
                p.PlayerIndex = i++;

                if (isFriendlyGame)
                {
                    var team = State.PlayersFriendlyTeam.GetValueOrDefault(p.AccountId, 0);

                    if (team >= 0 && team < capacities.Length)
                    {
                        var currentInTeam = friendlyTeamCount.GetValueOrDefault(team, 0);

                        if (currentInTeam < capacities[team])
                        {
                            p.TeamIndex = team;

                            friendlyTeamCount[team] = currentInTeam + 1;
                        }
                        else
                        {
                            p.TeamIndex = -1;

                            Logger.Warn($"Friendly team {team} is full. Player {p.AccountId} rejected.");
                        }
                    }
                    else
                    {
                        p.TeamIndex = -1;

                        Logger.Warn(
                            $"Friendly team {team} does not exist in GM {gameModeVar}. Player {p.AccountId} rejected.");
                    }
                }
                else
                {
                    p.TeamIndex = -1;
                }
            }

            if (!isFriendlyGame)
            {
                if (gameModeVar == 7)
                {
                    if (logicPlayers.Count > 0)
                    {
                        var bossIdx = Random.Shared.Next(logicPlayers.Count);

                        for (var ei = 0; ei < logicPlayers.Count; ei++)
                            logicPlayers[ei].TeamIndex = ei == bossIdx ? 0 : 1;
                    }
                }
                else
                {
                    var playersToAssign = logicPlayers.Where(p => p.TeamIndex == -1).ToList();

                    var playerGroups = playersToAssign
                        .GroupBy(p =>
                        {
                            var tid = State.PlayersTeam.GetValueOrDefault(p.AccountId);
                            return tid > 0 ? tid : -p.AccountId;
                        })
                        .Select(g => g.ToList())
                        .OrderByDescending(r => r.Count)
                        .ToList();

                    var teamOccupancy = new int[capacities.Length];

                    foreach (var group in playerGroups)
                        for (var tIdx = 0; tIdx < capacities.Length; tIdx++)
                        {
                            if (teamOccupancy[tIdx] + group.Count > capacities[tIdx])
                                continue;

                            foreach (var lp in group)
                                lp.TeamIndex = tIdx;

                            teamOccupancy[tIdx] += group.Count;
                            break;
                        }
                }
            }

            var failedPlayers = logicPlayers.Where(p => p.TeamIndex == -1).ToList();

            if (failedPlayers.Count > 0)
            {
                var failedIds = failedPlayers.Select(p => p.AccountId).ToHashSet();

                Logger.Warn($"Packing failed for {failedIds.Count} players in GameMode {gameModeVar}. Removing them.");

                logicPlayers.RemoveAll(p => failedIds.Contains(p.AccountId));

                State.PlayersTeam = State.PlayersTeam.Where(x => !failedIds.Contains(x.Key)).ToDictionary();
                State.PlayersFriendlyTeam =
                    State.PlayersFriendlyTeam.Where(x => !failedIds.Contains(x.Key)).ToDictionary();

                foreach (var room in State.Rooms.Values)
                    room.RemoveAll(id => failedIds.Contains(id));

                foreach (var room in State.FriendlyTeams.Values)
                    room.RemoveAll(id => failedIds.Contains(id));

                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                foreach (var id in failedIds)
                {
                    var h = GrainHelper.GetHomeGrain(GrainFactory, id);
                    await h.SendBattleServerErrorAsync(40);
                }
            }

            State.LogicPlayers = logicPlayers;

            State.BattleModeServer = new LogicBattleModeServer(State);

            var index = 0;

            foreach (var p in State.LogicPlayers)
            {
                var s = GenerateSessionId();

                State.PlayersArray[index] = (s, p.AccountId, p);

                State.PlayerAccountIdToIndex[p.AccountId] = index;
                State.PlayerSessionIdToIndex[s] = index;

                State.BattleServer.Sessions[s] = State.BattleModeServer;

                index++;
            }

            State.BattleActive = true;
            State.BattleStartTime = DateTime.UtcNow;

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var player in State.LogicPlayers)
            {
                var home = GrainHelper.GetHomeGrain(GrainFactory, player.AccountId);
                await home.InitiateBattleEntryAsync();
            }

            _dataRef = State;

            _ = looperPool.RegisterActionAsync(OnUpdate);

            return 0;
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
            return -11;
        }
    }

    public ValueTask<(bool, bool, int)> IsBattleActiveWithPlayersAsync(List<long> players)
    {
        var c = players.All(x => State.PlayersTeam.ContainsKey(x));

        return new ValueTask<(bool, bool, int)>((State is { BattleCreated: true, BattleActive: true }, c,
            State.ServerError));
    }

    public ValueTask<(string ip, int port, ulong lowSessionId, ushort highSessionId, byte[] kanan,
            PiranhaMessageStruct? startLoadingMessage)?>
        GetMyPlayerLoadingData(long accountId, bool showGameHints, byte b)
    {
        if (!State.BattleActive || State.BattleServer is null ||
            !State.PlayerAccountIdToIndex.TryGetValue(accountId, out var value))
            return ValueTask
                .FromResult<(string ip, int port, ulong lowSessionId, ushort highSessionId, byte[] kanan,
                    PiranhaMessageStruct?
                    startLoadingMessage)?>(null);

        ref var d = ref State.PlayersArray[value];

        var l = CreateStartLoadingMessage(accountId, false, showGameHints);
        PiranhaMessageStruct? msg = l != null ? LaserContractSerializer.SerializeToStruct(l) : null;

        var targetState = State.BCryptoStates.AddOrUpdate(d.id,
            id => new BCryptoState
            {
                AccountId = accountId,
                SessionId = id,
                Crypto = GoodbyeMyLoveGoodbye.CreateA9880()
            },
            (_, existingState) => existingState);

        var kanan = targetState.Crypto.R(b);

        return ValueTask
            .FromResult<(string ip, int port, ulong lowSessionId, ushort highSessionId, byte[] kanan,
                PiranhaMessageStruct? startLoadingMessage)?>((
                UdpBattleServerInstance.ServerIp, State.BattleServer.Port,
                d.id.Low, d.id.High, kanan, msg));
    }

    public ValueTask<(string ip, int port, ulong lowSessionId, ushort highSessionId, byte[] kanan,
            PiranhaMessageStruct? startLoadingMessage)?>
        GetMySpectatorLoadingData(long accountId, byte b)
    {
        if (!State.BattleActive || State.BattleServer is null ||
            !State.SpectatorAccountIdToState.TryGetValue(accountId, out var state))
            return ValueTask
                .FromResult<(string ip, int port, ulong lowSessionId, ushort highSessionId, byte[] kanan,
                    PiranhaMessageStruct? startLoadingMessage)?>(null);

        var l = CreateStartLoadingMessage(accountId, true, false);
        PiranhaMessageStruct? msg = l != null ? LaserContractSerializer.SerializeToStruct(l) : null;

        var targetState = State.BCryptoStates.AddOrUpdate(state.SessionId,
            id => new BCryptoState
            {
                AccountId = accountId,
                SessionId = id,
                Crypto = GoodbyeMyLoveGoodbye.CreateA9880()
            },
            (_, existingState) => existingState);

        var kanan = targetState.Crypto.R(b);

        return ValueTask
            .FromResult<(string ip, int port, ulong lowSessionId, ushort highSessionId, byte[] kanan,
                PiranhaMessageStruct? startLoadingMessage)?>((
                UdpBattleServerInstance.ServerIp, State.BattleServer.Port,
                state.SessionId.Low, state.SessionId.High, kanan, msg));
    }

    public ValueTask<int> AddSpectator(long playerAccountId, long accountId, bool brawlTv)
    {
        if (!State.PlayerAccountIdToIndex.ContainsKey(playerAccountId))
            return ValueTask.FromResult(-1);

        var sessionId = GenerateSessionId();

        if (State.SpectatorAccountIdToState.ContainsKey(accountId) ||
            State.SpectatorSessionIdToState.ContainsKey(sessionId))
            return ValueTask.FromResult(-2);

        var state = new SpectatorState
        {
            AccountId = accountId,
            SessionId = sessionId,
            LastInputIndex = 0
        };

        State.SpectatorAccountIdToState.TryAdd(accountId, state);
        State.SpectatorSessionIdToState.TryAdd(sessionId, state);

        var server = State.BattleServer;
        var modeServer = State.BattleModeServer;

        if (server != null && modeServer != null)
            server.Sessions.TryAdd(sessionId, modeServer);

        if (brawlTv)
            State.BrawlTv = true;

        return ValueTask.FromResult(0);
    }

    public ValueTask<bool> RemoveSpectator(long accountId)
    {
        if (!State.SpectatorAccountIdToState.TryRemove(accountId, out var state))
            return ValueTask.FromResult(false);

        State.SpectatorSessionIdToState.TryRemove(state.SessionId, out _);

        var server = State.BattleServer;

        if (server != null)
            server.Sessions.TryRemove(state.SessionId, out _);

        if (State.SpectatorAccountIdToState.IsEmpty)
            State.BrawlTv = false;

        return ValueTask.FromResult(true);
    }

    private bool OnUpdate(in LogicLooperActionContext ctx)
    {
        if (_dataRef?.BattleActive != true)
            return false;

        var result = OnTick();

        if (result)
            return result;

        if (_dataRef != null)
        {
            _dataRef.BattleActive = false;

            if (_dataRef.BattleServer != null)
            {
                foreach (var p in _dataRef.PlayersArray)
                {
                    if (p.id == default)
                        continue;

                    _dataRef.BattleServer.Sessions.TryRemove(p.id, out _);
                }

                foreach (var s in _dataRef.SpectatorAccountIdToState.Values)
                {
                    if (s.SessionId == default)
                        continue;

                    _dataRef.BattleServer.Sessions.TryRemove(s.SessionId, out _);
                }
            }

            _dataRef.BattleServer = null;
        }

        _dataRef = null;

        return result;
    }

    private bool OnTick()
    {
        if (_dataRef == null)
            return false;

        if (_dataRef.BattleServer == null)
        {
            _dataRef.ServerError = 41;
            return false;
        }

        if (_dataRef.BattleModeServer == null)
        {
            _dataRef.ServerError = 42;
            return false;
        }

        var resTick = _dataRef.BattleModeServer.ExecuteOneTick(out var exception);

        if (!exception)
            return resTick;

        _dataRef.ServerError = 43;
        return false;
    }

    private async Task<LogicPlayer?> GetPlayer(long accountId)
    {
        try
        {
            var gh = GrainHelper.GetHomeGrain(GrainFactory, accountId);

            return await gh.SetBattleAsync(this.GetPrimaryKey());
        }
        catch
        {
            return null;
        }
    }

    private StartLoadingMessage? CreateStartLoadingMessage(long forAccountId, bool spectator, bool showGameHints)
    {
        if (!State.BattleActive)
            return null;

        var my = State.LogicPlayers.FirstOrDefault(x => x.AccountId == forAccountId);

        if (my == null && !spectator)
            return null;

        var msg = new StartLoadingMessage
        {
            PlayersCount = State.LogicPlayers.Count,
            MyPlayerIndex = my?.PlayerIndex ?? 0,
            MyTeamIndex = my?.TeamIndex ?? 0,
            Players = State.LogicPlayers.ToArray(),
            EventModifiers = State.EventModifiers,
            GameType = 1,
            MapType = 1,
            ControlType = 1,
            GameHintsEnabled = showGameHints,
            SpectateMode = spectator ? 1 : 0,
            RaidDifficulty = State.Difficulty,
            LocationGlobalId = State.LocationGlobalId
        };

        return msg;
    }

    private static SessionId GenerateSessionId()
    {
        Span<byte> buffer = stackalloc byte[10];
        RandomNumberGenerator.Fill(buffer);

        var low = BinaryPrimitives.ReadUInt64LittleEndian(buffer[..8]);
        var high = BinaryPrimitives.ReadUInt16LittleEndian(buffer[8..]);

        return new SessionId(low, high);
    }
}