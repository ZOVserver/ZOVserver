using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NLog;
using ZOVserver.Services.Game.BattleService.Game.Input;
using ZOVserver.Services.Game.BattleService.Game.Objects;
using ZOVserver.Services.Game.BattleService.Game.Objects.Object.Derived;
using ZOVserver.Services.Game.BattleService.Game.TileMap;
using ZOVserver.Services.Game.BattleService.Network;
using ZOVserver.Services.Game.BattleService.States;
using ZOVserver.Shared.Contracts.Laser.Combined.Input;
using ZOVserver.Shared.Contracts.Laser.Messages.Server;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;
using ZOVserver.Shared.TitanRemnants.Streams;
using ZOVserver.Shared.TitanRemnants.Utility;

namespace ZOVserver.Services.Game.BattleService.Game;

public sealed class LogicBattleModeServer
{
    private const bool CheckClientInputIndex = false;
    private const float MaxShipTiltOffset = 15.0f;
    private const float ShipRotationSpeed = 2f;
    private const float ShipCenterStability = 1.0f;

    public const int IntroTicks = 120;
    public const int SpectatorDelayTicks = 100;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Queue<TickVisionSnapshotState> _battleHistory = new(128);

    private readonly ConcurrentQueue<InputEvent> _inputQueue = new();

    private float _currentShipTilt = 90.0f;

    public LogicBattleModeServer(BattleState state)
    {
        State = state;

        if (State.BattleServer == null)
            throw new Exception("State.BattleServer is null!");

        var mapName = LogicDataTables.GetDataById<LogicLocationData>(State.LocationGlobalId)?.AllowedMaps;

        if (mapName == null)
            throw new Exception($"Invalid map {mapName}!");

        var map = LogicDataTables.GetMapsManager(19)?.GetMapData(mapName);

        if (map == null)
            throw new Exception($"Invalid map data {mapName}!");

        TileMap = new LogicTileMap(map);

        GameObjectManagerServer = new LogicGameObjectManagerServer(this);

        GameOverState = -1;

        ShipModifierEnabled = state.EventModifiers.Contains(11);
    }

    public BattleState State { get; }

    public int CurrentTick { get; private set; }

    public LogicTileMap TileMap { get; }

    public int GameOverState { get; private set; }
    public int CoinRushEndTimer { get; private set; }

    public bool ShipModifierEnabled { get; }
    public int ShipAngle { get; private set; }

    public LogicGameObjectManagerServer GameObjectManagerServer { get; }

    /// <summary>
    ///     Processes a single simulation tick.
    ///     Invoked by the LogicLooper every 50ms (20 TPS).
    /// </summary>
    /// <returns>
    ///     <c>true</c> to continue the battle;
    ///     <c>false</c> to terminate and remove the session from the looper pool.
    /// </returns>
    public bool ExecuteOneTick(out bool exception)
    {
        try
        {
            exception = false;

            if (State.BattleServer == null || CurrentTick > 16380)
            {
                DisposeBattle();
                return false;
            }

            var tickRes = Tick();

            if (!tickRes)
            {
                DisposeBattle();
                return false;
            }

            VisionUpdateTick();

            ClearKillLists();

            CurrentTick++;

            return true;
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
            exception = true;

            DisposeBattle();
            return false;
        }
    }

    private void VisionUpdateTick()
    {
        SendVisionUpdateToPlayers();

        if (CurrentTick > SpectatorDelayTicks)
            SendVisionUpdateToSpectators();

        EnqueueBattleHistory();
    }

    private void SendVisionUpdateToPlayers()
    {
        var spectatorCount = State.SpectatorAccountIdToState.Count;
        var isBrawlTv = State.BrawlTv;
        var sessions = State.PlayersArray.AsSpan();

        for (var i = 0; i < sessions.Length; i++)
        {
            ref var s = ref sessions[i];

            if (s.id == default)
                continue;

            if (s.player.MyObjectRunningId < 1_000_000)
                continue;

            var b = new BitStream(512);

            GameObjectManagerServer.Encode(ref b, s.player.MyObjectRunningId, s.player.MyObjectIndex);

            var visionUpdateMessage = new VisionUpdateMessage
            {
                Tick = CurrentTick,
                Viewers = spectatorCount,
                BrawlTvMode = isBrawlTv,
                BitStreamBuffer = b.GetByteArray(),
                LastInput = s.player.LastInput
            };

            State.BattleServer!.SendMessage(s.id, visionUpdateMessage);

            b.Dispose();
        }
    }

    private void SendVisionUpdateToSpectators()
    {
        if (!_battleHistory.TryDequeue(out var snapshotState))
            return;

        foreach (var spectator in State.SpectatorAccountIdToState.Values)
        {
            var visionUpdateMessage = new VisionUpdateMessage
            {
                Tick = snapshotState.SnapshotTick,
                Viewers = snapshotState.SnapshotViewers,
                BrawlTvMode = snapshotState.SnapshotBrawlTv,
                BitStreamBuffer = snapshotState.SnapshotBitStreamBuffer,
                LastInput = spectator.LastInputIndex
            };

            State.BattleServer!.SendMessage(spectator.SessionId, visionUpdateMessage);
        }
    }

    private void EnqueueBattleHistory()
    {
        var spectatorCount = State.SpectatorAccountIdToState.Count;
        var isBrawlTv = State.BrawlTv;

        var b = new BitStream(512);

        GameObjectManagerServer.Encode(ref b, 0, -1);

        _battleHistory.Enqueue(new TickVisionSnapshotState
        {
            SnapshotTick = CurrentTick,
            SnapshotViewers = spectatorCount,
            SnapshotBrawlTv = isBrawlTv,
            SnapshotBitStreamBuffer = b.GetByteArray()
        });

        b.Dispose();
    }

    private void ClearKillLists()
    {
        var sessions = State.PlayersArray.AsSpan();

        for (var i = 0; i < sessions.Length; i++)
        {
            ref var s = ref sessions[i];

            if (s.id == default)
                continue;

            s.player.KillList.Clear();
        }
    }

    private void DisposeBattle()
    {
        _inputQueue.Clear();
    }

    private bool Tick()
    {
        HandleClientInputs();

        if (CurrentTick == 0)
            if (!FirstTick())
                return false;

        ShipTiltTick();

        GameObjectManagerServer.Tick();

        return true;
    }

    private bool FirstTick()
    {
        var players = CollectionsMarshal.AsSpan(State.LogicPlayers);

        ref readonly var spawn1Tiles = ref TileMap.GetDigitTiles('1');

        if (Unsafe.IsNullRef(in spawn1Tiles))
            return false;

        ref readonly var spawn2Tiles = ref TileMap.GetDigitTiles('2');

        if (LogicGameModeUtil.HasTwoTeams(State.GameMode))
            if (Unsafe.IsNullRef(in spawn2Tiles))
                return false;

        var hmtt = LogicGameModeUtil.HasMoreThanTwoTeams(State.GameMode);

        for (var i = 0; i < players.Length; i++)
        {
            ref var p = ref players[i];

            var c = new LogicCharacterServer();

            if (!c.BaseObject.InitObjectData(p.CharacterGlobalId))
                continue;

            var tiles = p.TeamIndex == 0 || hmtt ? spawn1Tiles : spawn2Tiles;
            var tile = tiles[p.PlayerIndex];

            c.BaseObject.Position = new Vector3(tile.LogicX, tile.LogicY, 0);
            c.BaseObject.Index = (uint)(p.PlayerIndex + 16 * p.TeamIndex);
            c.Player = p;

            var id = GameObjectManagerServer.AddLogicGameObject(in c);

            if (id <= 0)
                continue;

            p.MyObjectRunningId = (uint)id;
            p.MyObjectIndex = p.PlayerIndex + 16 * p.TeamIndex;
        }

        return true;
    }

    private void ShipTiltTick()
    {
        if (!(ShipModifierEnabled && CurrentTick > 120))
            return;

        var centerX = TileMap.Width / 2.0f;

        var centerCount = 0;
        var leftCount = 0;
        var rightCount = 0;

        var characters = GameObjectManagerServer.GetCharacters();

        foreach (ref var c in characters)
        {
            if (c.Player == null)
                continue;

            var tileX = LogicTileMap.LogicToTile(c.BaseObject.Position.X);

            if (Math.Abs(tileX - centerX) < ShipCenterStability)
                centerCount++;
            else if (tileX < centerX)
                leftCount++;
            else
                rightCount++;
        }

        var totalCount = centerCount + leftCount + rightCount;

        var targetAngle = 90.0f;

        if (totalCount > 0)
        {
            var balanceWeight = (float)(rightCount - leftCount) / totalCount;

            targetAngle = 90.0f + balanceWeight * MaxShipTiltOffset;
        }

        if (Math.Abs(_currentShipTilt - targetAngle) < ShipRotationSpeed)
            _currentShipTilt = targetAngle;
        else
            _currentShipTilt += _currentShipTilt < targetAngle ? ShipRotationSpeed : -ShipRotationSpeed;

        ShipAngle = (int)Math.Round(_currentShipTilt, MidpointRounding.AwayFromZero) - 90;
    }

    public void AddClientInput(in SessionId sessionId, ClientInput input)
    {
        _inputQueue.Enqueue(new InputEvent(sessionId, input));
    }

    private void HandleClientInputs()
    {
        while (_inputQueue.TryDequeue(out var item))
            HandleClientInput(item.SessionId, item.Input);
    }

    private void HandleClientInput(SessionId sessionId, ClientInput input)
    {
        if (!State.PlayerSessionIdToIndex.TryGetValue(sessionId, out var index))
        {
            if (State.SpectatorSessionIdToState.TryGetValue(sessionId, out var state))
                state.LastInputIndex = (int)input.Index;

            return;
        }

        ref var p = ref State.PlayersArray[index];

        if (p.id != sessionId)
            return;

        if (CheckClientInputIndex && input.Index > CurrentTick)
            return;

        p.player.LastInput = (int)input.Index;

        switch (input.Type)
        {
            case 1 or 0:
            {
                State.BattleActive = false;

                break;
            }
            case 2:
            {
                ref var character =
                    ref GameObjectManagerServer
                        .GetGameObjectById<LogicCharacterServer>((int)p.player.MyObjectRunningId);

                if (Unsafe.IsNullRef(ref character))
                    break;

                character.MoveTo(input.X, input.Y);

                break;
            }
        }
    }
}