using System.Collections.Concurrent;

namespace ZOVserver.Services.Game.MatchmakingService.States;

public class MatchmakingState
{
    public int SlotId { get; set; }
    public string Region { get; set; } = string.Empty;
    public int TrophiesSector { get; set; }
    public int MaxPlayers { get; set; }
    public int MaxPlayersInTeam { get; set; }

    public int LocationGlobalId { get; set; }
    public int GameModeVariation { get; set; }
    public int[] EventModifiers { get; set; } = [];
    public int Difficulty { get; set; }

    public ConcurrentDictionary<Guid, MatchmakeState> MatchmakeStates { get; set; } = [];
}