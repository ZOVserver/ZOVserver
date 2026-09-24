using Orleans;
using Orleans.Concurrency;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;
using ZOVserver.Shared.Contracts.Laser.Combined.Friend;
using ZOVserver.Shared.Contracts.Laser.Combined.Stream.Inheritors.Target;
using ZOVserver.Shared.Contracts.Laser.Combined.Team;

namespace ZOVserver.Shared.Contracts.Interfaces;

[Alias("ZOVserver.Shared.Contracts.Interfaces.ITeamServiceGrain")]
public interface ITeamServiceGrain : IGrainWithStringKey
{
    [Alias("CreateTeam")]
    public Task<int> CreateTeamAsync(TeamMemberEntry owner, int eventSlot, bool isFriendlyTeam);

    [Alias("JoinById")]
    public Task<int> JoinByIdAsync(TeamMemberEntry memberEntry);

    [Alias("Leave")]
    public Task<int> LeaveAsync(long memberId);

    [Alias("KickMember")]
    public Task<int> KickMemberAsync(long kickerId, long kickedId);

    [Alias("SetEvent")]
    public Task<int> SetEventAsync(int eventSlot, long accountId);

    [Alias("SetLocation")]
    public Task<int> SetLocationAsync(int locationGlobalId, long accountId);

    [Alias("SendPremadeStreamEntry")]
    public Task<int> SendPremadeStreamEntryAsync(long authorId, string authorName,
        int messageDataGlobalId, int eventSlot, int dataId, long targetPlayerId = 0);

    [Alias("SendTextStreamEntry")]
    public Task<int> SendTextStreamEntryAsync(long authorId, string authorName, string text);

    [Alias("SendEventStreamEntry")]
    public Task SendEventStreamEntryAsync(long authorId, string authorName, int eventType,
        EventStreamTargetEntry? targetEntry = null);

    [Alias("SetMemberData")]
    [OneWay]
    public Task SetMemberDataAsync(long memberId, TeamMemberData data);

    [Alias("ChangeMemberSettings")]
    public Task<int> ChangeMemberSettingsAsync(long memberId, TeamMemberData data);

    [Alias("TrySetMemberStatus")]
    public Task<bool> TrySetMemberStatusAsync(long accountId, int status);

    [Alias("SetMemberReady")]
    public Task<int> SetMemberReadyAsync(long memberId, bool isReady, string region);

    [Alias("ToggleMemberSide")]
    public Task<int> ToggleMemberSideAsync(long memberXId, long memberYId, int side);

    [Alias("AddInvite")]
    public Task<int> AddInviteAsync(FriendEntry inviter, long invitedId, int teamIndex);

    [Alias("ChangeInviteStatus")]
    public Task<int> ChangeInviteStatusAsync(int type, int type2, long accountId, TeamMemberData? d = null);

    [Alias("AddRequest")]
    public Task<int> AddRequestAsync(FriendEntry requester, long targetId);

    [Alias("ChangeRequestStatus")]
    public Task<int> ChangeRequestStatusAsync(long joinerId, int status);

    [OneWay]
    [Alias("RemoveAllPlayerPendingActions")]
    public Task RemoveAllPlayerPendingActionsAsync(long accountId);

    [Alias("SyncWithAlliance")]
    public Task<long[]> SyncWithAllianceAsync(long allianceId, long[] allianceMembers);

    [Alias("AddOrChangeMemberAllianceId")]
    public Task<int> AddOrChangeMemberAllianceIdAsync(long memberId, long allianceId);

    [Alias("CancelMatchmaking")]
    public Task<int> CancelMatchmakingAsync(bool cancelInMmGrain = true, bool fromHomeMessageManager = false);

    [Alias("IAmSolo")]
    public ValueTask<bool> IAmSolo(long memberId);

    [ReadOnly]
    [Alias("GetAllianceTeamEntry")]
    public ValueTask<AllianceTeamEntry> GetAllianceTeamEntry();

    [Alias("StartPlayersSearch")]
    public Task<int> StartPlayersSearchAsync(long memberId, int memberRegionGlobalId, int myTrophies);

    [Alias("StopPlayersSearch")]
    public Task<int> StopPlayersSearchAsync(long memberId);

    [Alias("JoinByTeamPlayersSearch")]
    public Task<int> JoinByTeamPlayersSearchAsync(TeamMemberEntry memberEntry);

    [Alias("ToBattleFromMatchmaking")]
    [OneWay]
    public Task ToBattleFromMatchmakingAsync(Guid id);
}