using System.Collections.Concurrent;
using ZOVserver.Services.Game.BattleService.Game;
using ZOVserver.Services.Game.BattleService.Network;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;

namespace ZOVserver.Services.Game.BattleService.States;

public class BattleState
{
    public bool BattleCreated { get; set; }
    public bool BattleActive { get; set; }
    public DateTime? BattleStartTime { get; set; }
    public int ServerError { get; set; }

    public bool IsFriendlyBattle { get; set; }

    public Dictionary<long, long> PlayersTeam { get; set; } = [];
    public Dictionary<long, int> PlayersFriendlyTeam { get; set; } = [];
    public Dictionary<long, List<long>> Rooms { get; set; } = [];
    public Dictionary<int, List<long>> FriendlyTeams { get; set; } = [];
    public List<LogicPlayer> LogicPlayers { get; set; } = [];

    public int GameMode { get; set; }
    public int LocationGlobalId { get; set; }
    public int[] EventModifiers { get; set; } = [];
    public int Difficulty { get; set; }

    public (SessionId id, long accountId, LogicPlayer player)[] PlayersArray { get; } =
        new (SessionId id, long accountId, LogicPlayer player)[16];

    public UdpBattleServerInstance? BattleServer { get; set; }
    public LogicBattleModeServer? BattleModeServer { get; set; }

    public bool BrawlTv { get; set; }

#pragma warning disable S3887
    public readonly Dictionary<long, int> PlayerAccountIdToIndex = new(16);
    public readonly Dictionary<SessionId, int> PlayerSessionIdToIndex = new(16);

    public readonly ConcurrentDictionary<long, SpectatorState> SpectatorAccountIdToState = new();
    public readonly ConcurrentDictionary<SessionId, SpectatorState> SpectatorSessionIdToState = new();

    public readonly ConcurrentDictionary<SessionId, BCryptoState> BCryptoStates =
        new();
#pragma warning restore S3887
}