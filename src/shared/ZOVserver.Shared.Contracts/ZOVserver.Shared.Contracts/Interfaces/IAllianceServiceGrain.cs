using Orleans;
using Orleans.Concurrency;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;
using ZOVserver.Shared.Contracts.Models;

namespace ZOVserver.Shared.Contracts.Interfaces;

[Alias("ZOVserver.Shared.Contracts.Interfaces.IAllianceServiceGrain")]
public interface IAllianceServiceGrain : IGrainWithStringKey
{
    [Alias("CreateAlliance")]
    public Task<AllianceParams?> CreateAllianceAsync(AllianceParams startParams, AllianceMember owner);

    [Alias("ChangeAllianceSettings")]
    public Task<int> ChangeAllianceSettingsAsync(long accountId, AllianceSettings settings);

    [Alias("UpdateAlliance")]
    public Task UpdateAllianceAsync(AllianceParams newParams);

    [Alias("GetAllianceInfo")]
    [ReadOnly]
    public Task<AllianceParams> GetAllianceInfoAsync();

    [Alias("GetAllianceInfoAndRole")]
    [ReadOnly]
    public Task<((AllianceParams, int, int), int?)> GetAllianceInfoAndRoleAsync(long accountId);

    [Alias("GetDetailedAllianceInfo")]
    [ReadOnly]
    public Task<DetailedAllianceModel> GetDetailedAllianceInfoAsync();

    [Alias("JoinMember")]
    public Task<int> JoinMemberAsync(AllianceMember member, DetailedAllianceMemberHomeModel model,
        long? actionerId = null, string? actionerName = null);

    [Alias("LeaveMember")]
    public Task<int> LeaveMemberAsync(long accountId, string name);

    [Alias("ChangeMemberRole")]
    public Task<int> ChangeMemberRoleAsync(long changerId, long changeableId, AllianceRole newRole,
        DetailedAllianceMemberHomeModel changerModel);

    [Alias("KickMember")]
    public Task<int> KickMemberAsync(long kickerId, string kickerName, long kickedId);

    [Alias("BanMember")]
    public Task<int> BanMemberAsync(long accountId, TimeSpan banTime);

    [Alias("GetMembers")]
    [ReadOnly]
    public Task<Dictionary<long, AllianceMember>> GetMembersAsync();

    [OneWay]
    [Alias("ChangeMemberStatus")]
    public Task ChangeMemberStatusAsync(long accountId, int status, bool inTeam);

    [OneWay]
    [Alias("ChangeMemberInfo")]
    public Task ChangeMemberInfoAsync(long accountId, DetailedAllianceMemberHomeModel model);

    [Alias("GetBannedMembers")]
    [ReadOnly]
    public Task<Dictionary<long, DateTime>> GetBannedMembersAsync();

    [Alias("SendTextStreamEntry")]
    [OneWay]
    public Task SendTextStreamEntryAsync(long authorId, string authorName, string text);

    [Alias("PlayerInTheClub")]
    [ReadOnly]
    public ValueTask<bool> PlayerInTheClub(long accountId);

    [Alias("SendJoinRequest")]
    public Task<int> SendJoinRequestAsync(long accountId, int nowTrophies, PlayerDisplayData displayData,
        string text);

    [Alias("JoinRequestAction")]
    public Task<int> JoinRequestActionAsync(long streamId, bool accepted, long actionerAccountId,
        string actionerName);

    [Alias("GetStreamEntryById")]
    [ReadOnly]
    public ValueTask<byte[]?> GetStreamEntryById(long id);

    [Alias("GetStreamEntries")]
    [ReadOnly]
    public ValueTask<List<byte[]>> GetStreamEntries();

    [Alias("GetActualTeams")]
    [ReadOnly]
    public Task<AllianceTeamEntry[]> GetActualTeamsAsync();

    [Alias("AddOrUpdateTeam")]
    [OneWay]
    public Task AddOrUpdateTeamAsync(AllianceTeamEntry teamEntry);

    [Alias("RemoveTeam")]
    [OneWay]
    public Task RemoveTeamAsync(long teamId);

    [Alias("GetAllianceRankingData")]
    [ReadOnly]
    public Task<AllianceRankingData?> GetAllianceRankingDataAsync();
}