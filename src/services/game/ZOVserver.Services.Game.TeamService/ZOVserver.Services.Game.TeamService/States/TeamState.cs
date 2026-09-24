using ZOVserver.Shared.Contracts.Laser.Combined.Stream;
using ZOVserver.Shared.Contracts.Laser.Combined.Team;

namespace ZOVserver.Services.Game.TeamService.States;

public class TeamState
{
    public DateTime? TeamCreatedTime { get; set; }

    public TeamEntry TeamEntry { get; set; } = new();
    public List<StreamEntry> StreamEntries { get; } = [];

    public List<long> KickedPlayers { get; } = [];
    public Dictionary<long, long> MemberAlliances { get; set; } = [];
}