using NLog;
using Orleans.Providers;
using ZOVserver.Services.Game.TeamService.Manager;
using ZOVserver.Services.Game.TeamService.Settings;
using ZOVserver.Services.Game.TeamService.States;
using ZOVserver.Shared.Abstractions;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Interfaces;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;
using ZOVserver.Shared.Contracts.Laser.Combined.Friend;
using ZOVserver.Shared.Contracts.Laser.Combined.Stream;
using ZOVserver.Shared.Contracts.Laser.Combined.Stream.Inheritors;
using ZOVserver.Shared.Contracts.Laser.Combined.Stream.Inheritors.Target;
using ZOVserver.Shared.Contracts.Laser.Combined.Team;
using ZOVserver.Shared.Contracts.Laser.Machine;
using ZOVserver.Shared.Contracts.Laser.Messages.Server;
using ZOVserver.Shared.Contracts.Structs;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;
using ZOVserver.Shared.TitanRemnants.Utility;

namespace ZOVserver.Services.Game.TeamService.Grains;

[StorageProvider(ProviderName = "MemoryStorage")]
// ReSharper disable once UnusedType.Global
public class TeamGrain(ITeamPlayersSearchService teamPlayersSearchService) : Grain<TeamState>, ITeamServiceGrain
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private bool _matchmakingStarted;
    private string? _playersSearchBucket;
    private string? _playersSearchTeamKey;
    private DateTime _lastPlayersSearchEndTime;
    private IDisposable? _tickTimer;

    private IMatchmakingServiceGrain? MatchmakingServiceGrain { get; set; }
    private Guid? MatchmakeId { get; set; }

    public async Task<int> CreateTeamAsync(TeamMemberEntry owner, int eventSlot, bool isFriendlyTeam)
    {
        if (State.TeamEntry.Members.Count > 0)
            return -1;

        if (!owner.IsOwner)
            return -2;

        State.StreamEntries.Clear();

        if (State.TeamEntry.Invites.Count > 0)
        {
            foreach (var inv in State.TeamEntry.Invites)
                _ = GrainHelper.GetHomeGrain(GrainFactory, inv.TargetId)
                    .TeamInviteCancelAsync(inv.TeamId);

            State.TeamEntry.Invites.Clear();
        }

        if (State.TeamEntry.JoinRequests.Count > 0)
        {
            PiranhaMessageStruct[] m =
                [LaserContractSerializer.SerializeToStruct(new TeamErrorMessage { ErrorCode = 12 })];

            foreach (var jr in State.TeamEntry.JoinRequests)
                _ = GrainHelper.GetPlayerSession(GrainFactory, jr.JoinerId)
                    .SendMessagesToPlayerAsync(m);

            State.TeamEntry.JoinRequests.Clear();
        }

        State.TeamEntry.Members.Add(owner);

        State.TeamEntry.RoomType = isFriendlyTeam ? 1 : 0;
        State.TeamEntry.IsFriendlyRoom = isFriendlyTeam;

        State.TeamCreatedTime = DateTime.UtcNow;

        await SendEventStreamEntryAsync(owner.AccountId, owner.DisplayData.AvatarName, 101);

        _tickTimer ??= this.RegisterGrainTimer<object?>(
            async _ => await TickAsync(),
            null,
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromSeconds(30),
                Period = TimeSpan.FromSeconds(30),
                Interleave = false
            });

        var r = -100 + await SetEventAsync(eventSlot, owner.AccountId);

        if (r != -100)
            await DeleteTeamAsync();

        return r;
    }

    public async Task<int> JoinByIdAsync(TeamMemberEntry memberEntry)
    {
        if (State.TeamCreatedTime == null)
            return -1;

        if (State.TeamEntry.Members.Count == 0)
            return -2;

        if (State.KickedPlayers.Contains(memberEntry.AccountId))
            return -3;

        if (memberEntry.IsOwner)
            return -4;

        if (State.TeamEntry.Members.Any(x => x.AccountId == memberEntry.AccountId))
            return -5;

        var maxPlayers = LogicGamePlayUtil.GetPlayerCountWithGameModeVariation(State.TeamEntry.GameModeVariation,
            State.TeamEntry.IsFriendlyRoom);

        if (State.TeamEntry.Members.Count >= maxPlayers)
            return -6;

        var index = GetTeamIndexForNewPlayer();

        if (index < 0)
            return -6;

        var inv = State.TeamEntry.Invites.FirstOrDefault(x => x.TargetId == memberEntry.AccountId);

        if (inv != null)
        {
            _ = GrainHelper.GetHomeGrain(GrainFactory, inv.TargetId).TeamInviteCancelAsync(inv.TeamId);

            State.TeamEntry.Invites.Remove(inv);
        }

        var req = State.TeamEntry.JoinRequests.FirstOrDefault(x => x.JoinerId == memberEntry.AccountId);

        if (req != null)
        {
            _ = GrainHelper.GetHomeGrain(GrainFactory, req.JoinerId).RemoveTeamRequest(State.TeamEntry.TeamId, 3);

            State.TeamEntry.JoinRequests.Remove(req);
        }

        memberEntry.TeamIndex = index;

        State.TeamEntry.Members.Add(memberEntry);

        var maxPlayersInTeam = LogicGamePlayUtil.GetPlayerCountInTeamWithGameModeVariation(
            State.TeamEntry.GameModeVariation, State.TeamEntry.IsFriendlyRoom);

        if (State.TeamEntry.Members.Count(x => x.TeamIndex == index) >= maxPlayersInTeam)
            foreach (var invite in State.TeamEntry.Invites
                         .Where(x => x.TeamIndex == index).ToArray())
            {
                _ = GrainHelper.GetHomeGrain(GrainFactory, invite.TargetId).TeamInviteCancelAsync(invite.TeamId);

                State.TeamEntry.Invites.Remove(invite);
            }

        if (State.TeamEntry.Members.Count >= State.TeamEntry.MaxPlayers)
        {
            foreach (var invite in State.TeamEntry.Invites.ToArray())
            {
                _ = GrainHelper.GetHomeGrain(GrainFactory, invite.TargetId).TeamInviteCancelAsync(invite.TeamId);

                State.TeamEntry.Invites.Remove(invite);
            }

            foreach (var request in State.TeamEntry.JoinRequests.ToArray())
            {
                _ = GrainHelper.GetHomeGrain(GrainFactory, request.JoinerId)
                    .RemoveTeamRequest(State.TeamEntry.TeamId, 2);

                State.TeamEntry.JoinRequests.Remove(request);
            }
        }

        foreach (var m in State.TeamEntry.Members)
            m.IsReady = false;

        await SendTeamMessageToMembersAsync();

        await SendEventStreamEntryAsync(memberEntry.AccountId, memberEntry.DisplayData.AvatarName, 102);

        await UpdateAllianceTeamInAllAlliancesAsync();
        await UpdateAllianceTeamInAllFriendshipsAsync();

        _ = StopPlayerSearchAsync();
        _ = CancelMatchmakingAsync();

        return 0;
    }

    public async Task<int> LeaveAsync(long memberId)
    {
        var member = State.TeamEntry.Members.FirstOrDefault(x => x.AccountId == memberId);

        if (member == null)
            return -1;

        foreach (var m in State.TeamEntry.Members)
            m.IsReady = false;

        _ = StopPlayerSearchAsync();
        _ = CancelMatchmakingAsync();

        State.TeamEntry.Members.Remove(member);

        await GrainHelper.GetPlayerSession(GrainFactory, memberId).SendMessagesToPlayerAsync([
            LaserContractSerializer.SerializeToStruct(new TeamLeftMessage { Reason = 0 })
        ]);

        foreach (var inv in State.TeamEntry.Invites.ToArray())
        {
            if (inv.InviterId != memberId) continue;

            _ = GrainHelper.GetHomeGrain(GrainFactory, inv.TargetId).TeamInviteCancelAsync(inv.TeamId);

            State.TeamEntry.Invites.Remove(inv);
        }

        foreach (var req in State.TeamEntry.JoinRequests.ToArray())
        {
            if (req.TargetId != memberId) continue;

            _ = GrainHelper.GetHomeGrain(GrainFactory, req.JoinerId).RemoveTeamRequest(State.TeamEntry.TeamId, 3);

            State.TeamEntry.JoinRequests.Remove(req);
        }

        if (State.TeamEntry.Members.Count == 0)
        {
            await RemoveAllianceTeamInFriendshipAsync(memberId);

            await DeleteTeamAsync();
            return 0;
        }

        if (member.IsOwner)
            State.TeamEntry.Members.First().IsOwner = true;

        await SendEventStreamEntryAsync(member.AccountId, member.DisplayData.AvatarName, 103);

        await SendTeamMessageToMembersAsync();

        await UpdateAllianceTeamInAllAlliancesAsync();
        await UpdateAllianceTeamInAllFriendshipsAsync();

        var r = State.MemberAlliances.Remove(memberId, out var allianceId);

        if (r && State.MemberAlliances.All(x => x.Value != allianceId))
            await RemoveAllianceTeamInAllianceAsync(allianceId);

        await RemoveAllianceTeamInFriendshipAsync(memberId);

        return 0;
    }

    public async Task<int> KickMemberAsync(long kickerId, long kickedId)
    {
        var kicker = State.TeamEntry.Members.FirstOrDefault(x => x.AccountId == kickerId);

        if (kicker == null)
            return -1;

        if (!kicker.IsOwner)
            return -2;

        var kicked = State.TeamEntry.Members.FirstOrDefault(x => x.AccountId == kickedId);

        if (kicked == null)
            return -3;

        if (kicked.IsOwner)
            return -4;

        var c = State.TeamEntry.Members.RemoveAll(x => x.AccountId == kickedId);
        if (c <= 0) return -5;

        State.KickedPlayers.Add(kickedId);

        foreach (var m in State.TeamEntry.Members)
            m.IsReady = false;

        _ = StopPlayerSearchAsync();
        _ = CancelMatchmakingAsync();

        _ = GrainHelper.GetHomeGrain(GrainFactory, kickedId).KickFromTeamAsync(State.TeamEntry.TeamId);

        await SendEventStreamEntryAsync(kicked.AccountId, kicked.DisplayData.AvatarName, 104,
            new EventStreamTargetEntry { AccountId = kicker.AccountId, Name = kicker.DisplayData.AvatarName });

        foreach (var inv in State.TeamEntry.Invites.ToArray())
        {
            if (inv.InviterId != kickedId) continue;

            _ = GrainHelper.GetHomeGrain(GrainFactory, inv.TargetId).TeamInviteCancelAsync(inv.TeamId);

            State.TeamEntry.Invites.Remove(inv);
        }

        foreach (var req in State.TeamEntry.JoinRequests.ToArray())
        {
            if (req.TargetId != kickedId) continue;

            _ = GrainHelper.GetHomeGrain(GrainFactory, req.JoinerId).RemoveTeamRequest(State.TeamEntry.TeamId, 3);

            State.TeamEntry.JoinRequests.Remove(req);
        }

        await SendTeamMessageToMembersAsync();

        await UpdateAllianceTeamInAllAlliancesAsync();
        await UpdateAllianceTeamInAllFriendshipsAsync();

        var r = State.MemberAlliances.Remove(kickedId, out var allianceId);

        if (r && State.MemberAlliances.All(x => x.Value != allianceId))
            await RemoveAllianceTeamInAllianceAsync(allianceId);

        await RemoveAllianceTeamInFriendshipAsync(kickedId);

        return 0;
    }

    public async Task<int> SetEventAsync(int eventSlot, long accountId)
    {
        if (State.TeamEntry.Members.FirstOrDefault(x => x.AccountId == accountId)?.IsOwner != true)
            return -1;

        var events = EventsManager.GetEvents().ToArray();

        var eventData = events.FirstOrDefault(x => x.SlotId == eventSlot);

        if (eventData == null)
            return -2;

        var location = LogicDataTables.GetDataById<LogicLocationData>(eventData.LocationGlobalId);

        if (location == null)
            return -3;

        var gmodevar = LogicDataTables.GetDataByName<LogicGameModeVariationData>(
            GameModeToVariationConverter.GetGameModeVariation(location.GameMode));

        if (gmodevar == null)
            return -4;

        var maxPlayers =
            LogicGamePlayUtil.GetPlayerCountWithGameModeVariation(gmodevar.Variation,
                State.TeamEntry.IsFriendlyRoom);

        if (maxPlayers < 2)
            return -5;

        if (State.TeamEntry.Members.Count > maxPlayers || State.TeamEntry.Members.All(x => x.IsReady))
            return -6;

        if (State.TeamEntry.Invites.Count > 0)
            return -7;

        _ = StopPlayerSearchAsync();
        _ = CancelMatchmakingAsync();

        foreach (var m in State.TeamEntry.Members)
            m.IsReady = false;

        State.TeamEntry.GameModeVariation = gmodevar.Variation;
        State.TeamEntry.MaxPlayers = maxPlayers;
        State.TeamEntry.LocationGlobalId = eventData.LocationGlobalId;
        State.TeamEntry.EventSlot = eventSlot;

        UpdateTeamMemberIndexes();

        await UpdateAllianceTeamInAllAlliancesAsync();
        await UpdateAllianceTeamInAllFriendshipsAsync();

        await SendTeamMessageToMembersAsync();
        return 0;
    }

    public async Task<int> SetLocationAsync(int locationGlobalId, long accountId)
    {
        if (State.TeamEntry.Members.FirstOrDefault(x => x.AccountId == accountId)?.IsOwner != true)
            return -1;

        if (State.TeamEntry.RoomType != 1)
            return -2;

        var location = LogicDataTables.GetDataById<LogicLocationData>(locationGlobalId);

        if (location == null)
            return -3;

        if (location.Disabled)
            return -4;

        var gmodevar = LogicDataTables.GetDataByName<LogicGameModeVariationData>(
            GameModeToVariationConverter.GetGameModeVariation(location.GameMode));

        if (gmodevar == null)
            return -4;

        var maxPlayers =
            LogicGamePlayUtil.GetPlayerCountWithGameModeVariation(gmodevar.Variation,
                State.TeamEntry.IsFriendlyRoom);

        if (maxPlayers < 2)
            return -5;

        if (State.TeamEntry.Members.Count > maxPlayers || State.TeamEntry.Members.All(x => x.IsReady))
            return -6;

        if (State.TeamEntry.Invites.Count > 0)
            return -7;

        _ = StopPlayerSearchAsync();
        _ = CancelMatchmakingAsync();

        foreach (var m in State.TeamEntry.Members)
            m.IsReady = false;

        State.TeamEntry.GameModeVariation = gmodevar.Variation;
        State.TeamEntry.MaxPlayers = maxPlayers;
        State.TeamEntry.LocationGlobalId = locationGlobalId;

        UpdateTeamMemberIndexes();

        await UpdateAllianceTeamInAllAlliancesAsync();
        await UpdateAllianceTeamInAllFriendshipsAsync();

        await SendTeamMessageToMembersAsync();
        return 0;
    }

    public async Task<int> SendPremadeStreamEntryAsync(long authorId, string authorName,
        int messageDataGlobalId, int eventSlot, int dataId, long targetPlayerId = 0)
    {
        var author = State.TeamEntry.Members.FirstOrDefault(x => x.AccountId == authorId);

        if (author == null)
            return -1;

        var msg = LogicDataTables.GetDataById<LogicMessageData>(messageDataGlobalId);

        if (msg == null || msg.Disabled)
            return -1;

        if (messageDataGlobalId is 40_000_001 or 40_000_036)
        {
            if (State.TeamEntry.Invites.Count > 0)
                return -2;

            var location = LogicDataTables.GetDataById<LogicLocationData>(dataId);

            if (location == null)
                return -3;

            var gmodevar = LogicDataTables.GetDataByName<LogicGameModeVariationData>(
                GameModeToVariationConverter.GetGameModeVariation(location.GameMode));

            if (gmodevar == null)
                return -4;

            var maxPlayers =
                LogicGamePlayUtil.GetPlayerCountWithGameModeVariation(gmodevar.Variation,
                    State.TeamEntry.IsFriendlyRoom);

            if (State.TeamEntry.Members.Count > maxPlayers)
                return -5;
        }

        var target = targetPlayerId > 0
            ? new TeamPremadeChatTargetPlayer { PlayerId = targetPlayerId }
            : null;

        var chatEntry = new QuickChatStreamEntry
        {
            AuthorId = authorId,
            AuthorName = authorName,
            MessageDataGlobalId = messageDataGlobalId,
            EventSlot = eventSlot,
            DataId = dataId,
            TargetPlayer = target
        };

        await SendStreamEntryAsync(chatEntry);
        return 0;
    }

    public async Task<int> SendTextStreamEntryAsync(long authorId, string authorName, string text)
    {
        var author = State.TeamEntry.Members.FirstOrDefault(x => x.AccountId == authorId);

        if (author == null)
            return -1;

        var chatEntry = new ChatStreamEntry
        {
            AuthorId = authorId,
            AuthorName = authorName,
            Text = text
        };

        await SendStreamEntryAsync(chatEntry);
        return 0;
    }

    public async Task SendEventStreamEntryAsync(long authorId, string authorName, int eventType,
        EventStreamTargetEntry? targetEntry = null)
    {
        var author = State.TeamEntry.Members.FirstOrDefault(x => x.AccountId == authorId);

        if (author == null)
            return;

        var eventEntry = new AllianceEventStreamEntry
        {
            AuthorId = authorId,
            AuthorName = authorName,
            EventType = eventType,
            TargetEntry = targetEntry
        };

        await SendStreamEntryAsync(eventEntry);
    }

    public async Task<int> CancelMatchmakingAsync(bool cancelInMmGrain = true, bool fromHomeMessageManager = false)
    {
        var member = State.TeamEntry.Members.FirstOrDefault(m => m.IsOwner);

        if (member == null)
            return -1;

        if (!_matchmakingStarted)
        {
            if (IsPlayerSearchBug1WhenTeamIsFull())
                return 0;

            MatchmakeId = null;
            MatchmakingServiceGrain = null;

            return -2;
        }

        if (fromHomeMessageManager)
            await SendMessagesToMembersAsync([
                LaserContractSerializer.SerializeToStruct(new MatchmakeFailedMessage { ErrorCode = 7 })
            ]);

        if (MatchmakeId != null && MatchmakingServiceGrain != null)
            try
            {
                if (cancelInMmGrain)
                {
                    var ms = State.TeamEntry.Members.Select(x => x.AccountId).ToList();
                    var r = await MatchmakingServiceGrain.RemovePlayersFromMatchmakingAsync(MatchmakeId.Value, ms);

                    if (r == -2)
                        return 0;
                }

                MatchmakingServiceGrain = null;
                MatchmakeId = null;
            }
            catch
            {
                MatchmakingServiceGrain = null;
                MatchmakeId = null;
            }

        foreach (var m in State.TeamEntry.Members)
            m.IsReady = false;

        await SendEventStreamEntryAsync(member.AccountId, member.DisplayData.AvatarName, 106);

        await SendMessagesToMembersAsync([
            LaserContractSerializer.SerializeToStruct(new MatchMakingCancelledMessage()),
            LaserContractSerializer.SerializeToStruct(new TeamMessage { TeamEntry = State.TeamEntry })
        ]);

        _matchmakingStarted = false;

        return 0;
    }

    public async Task SetMemberDataAsync(long memberId, TeamMemberData data)
    {
        var member = State.TeamEntry.Members.FirstOrDefault(m => m.AccountId == memberId);

        if (member == null)
            return;

        member.DisplayData = data.DisplayData;

        member.CharacterGlobalId = data.CharacterGlobalId;
        member.SkinGlobalId = data.SkinGlobalId;

        member.HeroTrophies = data.HeroTrophies;
        member.HeroMaxTrophies = data.HeroMaxTrophies;
        member.HeroPowerLevel = data.HeroPowerLevel;

        member.StarPowerGlobalId = data.StarPowerGlobalId;

        member.DifficultyLevel = data.DifficultyLevel;

        await SendTeamMessageToMembersAsync();
    }

    public async Task<int> ChangeMemberSettingsAsync(long memberId, TeamMemberData data)
    {
        var member = State.TeamEntry.Members.FirstOrDefault(m => m.AccountId == memberId);

        if (member == null)
            return -1;

        member.DisplayData = data.DisplayData;

        member.CharacterGlobalId = data.CharacterGlobalId;
        member.SkinGlobalId = data.SkinGlobalId;

        member.HeroTrophies = data.HeroTrophies;
        member.HeroMaxTrophies = data.HeroMaxTrophies;
        member.HeroPowerLevel = data.HeroPowerLevel;

        member.StarPowerGlobalId = data.StarPowerGlobalId;

        member.DifficultyLevel = data.DifficultyLevel;

        await SendTeamMessageToMembersAsync();
        return 0;
    }

    public async Task<bool> TrySetMemberStatusAsync(long accountId, int status)
    {
        if (State.TeamEntry.Members.All(x => x.AccountId != accountId))
            return false;

        await SetMemberStatusAsync(accountId, status);
        return true;
    }

    public async Task<int> SetMemberReadyAsync(long memberId, bool isReady, string region)
    {
        if (EventsManager.GetMaintenanceSecondsLeft() > 0)
            return -4;

        var member = State.TeamEntry.Members.FirstOrDefault(m => m.AccountId == memberId);

        if (member == null)
            return -1;

        if (member.IsOwner && IsPlayersSearchStarted())
            return -1;

        var d = State.TeamEntry.Members
            .GroupBy(p => p.CharacterGlobalId)
            .Any(g => g.Count() > 1);

        if (d && !State.TeamEntry.IsFriendlyRoom)
            return -2;

        if (State.TeamEntry.Invites.Count > 0)
            return -3;

        member.IsReady = isReady;

        await SendTeamMessageToMembersAsync();

        if (State.TeamEntry.Members.All(x => x.IsReady))
            return 1000 + await StartMatchmakingAsync(region);

        return 0;
    }

    public async Task<int> ToggleMemberSideAsync(long memberXId, long memberYId, int side)
    {
        if (!State.TeamEntry.IsFriendlyRoom)
            return -1;

        var memberX = State.TeamEntry.Members.FirstOrDefault(m => m.AccountId == memberXId);

        if (memberX == null)
            return -1;

        if (memberYId > 0)
        {
            var memberY = State.TeamEntry.Members.FirstOrDefault(m => m.AccountId == memberYId);

            if (memberY == null)
                return -2;

            var indexX = State.TeamEntry.Members.IndexOf(memberX);
            var indexY = State.TeamEntry.Members.IndexOf(memberY);

            (memberX.TeamIndex, memberY.TeamIndex) = (memberY.TeamIndex, memberX.TeamIndex);

            State.TeamEntry.Members[indexX] = memberY;
            State.TeamEntry.Members[indexY] = memberX;

            await SendTeamMessageToMembersAsync();
            return 0;
        }

        switch (State.TeamEntry.GameModeVariation)
        {
            case 6:
            case 8:
            case 10:
            case 13:
            case 14:
            case 15:
            {
                memberX.TeamIndex = 0;
                break;
            }

            case 0:
            case 2:
            case 3:
            case 5:
            case 11:
            case 16:
            {
                if (side is < 0 or > 5)
                    return -3;

                var teamIndexTrio = side < 3 ? 0 : 1;

                if (State.TeamEntry.Members.Count(x => x.TeamIndex == teamIndexTrio) +
                    State.TeamEntry.Invites.Count(x => x.TeamIndex == teamIndexTrio) >= 3)
                    return -4;

                memberX.TeamIndex = teamIndexTrio;

                State.TeamEntry.Members.Remove(memberX);

                var lastIndexTrio = -1;

                for (var i = State.TeamEntry.Members.Count - 1; i >= 0; i--)
                {
                    if (State.TeamEntry.Members[i].TeamIndex != memberX.TeamIndex) continue;

                    lastIndexTrio = i;
                    break;
                }

                if (lastIndexTrio != -1)
                    State.TeamEntry.Members.Insert(lastIndexTrio + 1, memberX);
                else
                    State.TeamEntry.Members.Add(memberX);

                break;
            }

            case 9:
            {
                if (side is < 0 or > 9)
                    return -3;

                var teamIndexDuo = side / 2;

                if (State.TeamEntry.Members.Count(x => x.TeamIndex == teamIndexDuo) +
                    State.TeamEntry.Invites.Count(x => x.TeamIndex == teamIndexDuo) >= 2)
                    return -4;

                memberX.TeamIndex = teamIndexDuo;

                State.TeamEntry.Members.Remove(memberX);

                var lastIndexDuo = -1;

                for (var i = State.TeamEntry.Members.Count - 1; i >= 0; i--)
                {
                    if (State.TeamEntry.Members[i].TeamIndex != memberX.TeamIndex) continue;

                    lastIndexDuo = i;
                    break;
                }

                if (lastIndexDuo != -1)
                    State.TeamEntry.Members.Insert(lastIndexDuo + side % 2, memberX);
                else
                    State.TeamEntry.Members.Add(memberX);

                break;
            }

            case 7:
            {
                switch (side)
                {
                    case < 0 or > 5:
                        return -3;
                    case > 0:
                        side = 1;
                        break;
                }

                if (State.TeamEntry.Members.Count(x => x.TeamIndex == side) +
                    State.TeamEntry.Invites.Count(x => x.TeamIndex == side) >= (side == 0 ? 1 : 5))
                    return -4;

                foreach (var m in State.TeamEntry.Members)
                    m.TeamIndex = 1;

                memberX.TeamIndex = side;

                if (State.TeamEntry.Members.All(x => x.TeamIndex == 1))
                    State.TeamEntry.Members[0].TeamIndex = 0;

                break;
            }

            default:
                throw new ArgumentException($"Unknown game mode variation: {State.TeamEntry.GameModeVariation}");
        }

        await SendTeamMessageToMembersAsync();
        return 0;
    }

    public async Task<int> AddInviteAsync(FriendEntry inviter, long invitedId, int teamIndex)
    {
        var member = State.TeamEntry.Members.FirstOrDefault(m => m.AccountId == inviter.AccountId);

        if (member == null)
            return -1;

        if (State.TeamEntry.Members.Count >= State.TeamEntry.MaxPlayers)
            return -2;

        var invitedHome = GrainHelper.GetHomeGrain(GrainFactory, invitedId);

        var res = await invitedHome.GetIsPossibleToTeamInviteAndAvatarName(State.TeamEntry.TeamId, inviter.AccountId,
            inviter.Alliance?.AllianceId ?? 0);

        switch (res.Item1)
        {
            case -7: // invites blocked
                return -3;
            case -6: // in team
                return -4;
            case -5: // offline
                return -5;
            case -4: // muted
                return -6;
            case -3: // bot or empty account
                return -7;
            case -2: // invalid alliance
                return -8;
            case -1: // no friends
                return -9;
        }

        if (State.TeamEntry.Invites.Count > 10)
            return -10;

        if (State.TeamEntry.Invites.Any(x => x.TargetId == invitedId))
            return -11;

        var c = CheckInviteTeamIndex(teamIndex);

        if (c != 0)
            return c;

        var s = await invitedHome.TeamInviteAsync(State.TeamEntry.TeamId, inviter);

        if (s != 0)
            return -21;

        State.TeamEntry.Invites.Add(new TeamInviteEntry
        {
            InviterId = inviter.AccountId,
            TargetId = invitedId,
            TargetName = res.Item2,
            TeamIndex = teamIndex,
            TeamId = State.TeamEntry.TeamId,
            Status = 1,
            InviterName = inviter.DisplayData?.AvatarName ?? "inviter"
        });

        await SendTeamMessageToMembersAsync();
        return 0;
    }

    public async Task<int> ChangeInviteStatusAsync(int type, int type2, long accountId, TeamMemberData? d = null)
    {
        var invite = State.TeamEntry.Invites.FirstOrDefault(x => x.TargetId == accountId);

        if (invite == null)
            return -1;

        switch (type)
        {
            case 1: // clear invite
            {
                State.TeamEntry.Invites.RemoveAll(x => x.TargetId == accountId);

                _ = GrainHelper.GetHomeGrain(GrainFactory, invite.TargetId).TeamInviteCancelAsync(invite.TeamId);

                if (type2 == 1)
                    await GrainHelper.GetPlayerSession(GrainFactory, invite.InviterId).SendMessagesToPlayerAsync([
                        LaserContractSerializer.SerializeToStruct(new TeamErrorMessage { ErrorCode = 29 })
                    ]);

                await SendTeamMessageToMembersAsync();
                return 0;
            }
            case 2:
            {
                State.TeamEntry.Invites.RemoveAll(x => x.TargetId == accountId);

                var dr = State.TeamEntry.JoinRequests.RemoveAll(x => x.JoinerId == accountId);

                await SendTeamMessageToMembersAsync();

                switch (type2)
                {
                    case 1: // y
                    {
                        if (State.TeamEntry.Members.All(x => x.AccountId != invite.InviterId))
                        {
                            _ = GrainHelper.GetHomeGrain(GrainFactory, accountId)
                                .TeamInviteCancelAsync(State.TeamEntry.TeamId);
                            return -2;
                        }

                        if (State.TeamEntry.Members.Any(x => x.AccountId == invite.TargetId))
                        {
                            _ = GrainHelper.GetHomeGrain(GrainFactory, accountId)
                                .TeamInviteCancelAsync(State.TeamEntry.TeamId);
                            return -3;
                        }

                        var maxPlayersInTeam =
                            LogicGamePlayUtil.GetPlayerCountInTeamWithGameModeVariation(
                                State.TeamEntry.GameModeVariation, State.TeamEntry.IsFriendlyRoom);

                        if (State.TeamEntry.Members.Count(x => x.TeamIndex == invite.TeamIndex) >= maxPlayersInTeam)
                        {
                            _ = GrainHelper.GetHomeGrain(GrainFactory, accountId)
                                .TeamInviteCancelAsync(State.TeamEntry.TeamId);
                            return -4;
                        }

                        if (dr > 0)
                            _ = GrainHelper.GetHomeGrain(GrainFactory, accountId)
                                .RemoveTeamRequest(State.TeamEntry.TeamId, 2);

                        var memberEntry = new TeamMemberEntry
                        {
                            AccountId = invite.TargetId
                        };

                        if (d == null)
                        {
                            _ = GrainHelper.GetHomeGrain(GrainFactory, accountId)
                                .TeamInviteCancelAsync(State.TeamEntry.TeamId);
                            return -5;
                        }

                        memberEntry.DisplayData = d.DisplayData;

                        memberEntry.CharacterGlobalId = d.CharacterGlobalId;
                        memberEntry.SkinGlobalId = d.SkinGlobalId;

                        memberEntry.HeroTrophies = d.HeroTrophies;
                        memberEntry.HeroMaxTrophies = d.HeroMaxTrophies;
                        memberEntry.HeroPowerLevel = d.HeroPowerLevel;

                        memberEntry.StarPowerGlobalId = d.StarPowerGlobalId;
                        memberEntry.DifficultyLevel = d.DifficultyLevel;

                        memberEntry.TeamIndex = invite.TeamIndex;
                        memberEntry.State = 3;

                        State.TeamEntry.Members.Add(memberEntry);

                        if (State.TeamEntry.Members.Count(x => x.TeamIndex == invite.TeamIndex) >= maxPlayersInTeam)
                            foreach (var inv in State.TeamEntry.Invites
                                         .Where(x => x.TeamIndex == invite.TeamIndex).ToArray())
                            {
                                _ = GrainHelper.GetHomeGrain(GrainFactory, inv.TargetId)
                                    .TeamInviteCancelAsync(inv.TeamId);

                                State.TeamEntry.Invites.Remove(inv);
                            }

                        if (State.TeamEntry.Members.Count >= State.TeamEntry.MaxPlayers)
                        {
                            foreach (var inv in State.TeamEntry.Invites.ToArray())
                            {
                                _ = GrainHelper.GetHomeGrain(GrainFactory, inv.TargetId)
                                    .TeamInviteCancelAsync(inv.TeamId);

                                State.TeamEntry.Invites.Remove(inv);
                            }

                            foreach (var req in State.TeamEntry.JoinRequests.ToArray())
                            {
                                _ = GrainHelper.GetHomeGrain(GrainFactory, req.JoinerId)
                                    .RemoveTeamRequest(State.TeamEntry.TeamId, 2);

                                State.TeamEntry.JoinRequests.Remove(req);
                            }
                        }

                        await SendTeamMessageToMembersAsync();

                        await SendEventStreamEntryAsync(memberEntry.AccountId, memberEntry.DisplayData.AvatarName, 102);

                        await UpdateAllianceTeamInAllAlliancesAsync();
                        await UpdateAllianceTeamInAllFriendshipsAsync();

                        _ = StopPlayerSearchAsync();
                        _ = CancelMatchmakingAsync();

                        return 0;
                    }
                    case 2: // n
                    {
                        if (dr > 0)
                            _ = GrainHelper.GetHomeGrain(GrainFactory, accountId)
                                .RemoveTeamRequest(State.TeamEntry.TeamId, 2);

                        await GrainHelper.GetPlayerSession(GrainFactory, invite.InviterId).SendMessagesToPlayerAsync([
                            LaserContractSerializer.SerializeToStruct(new TeamErrorMessage { ErrorCode = 26 })
                        ]);

                        return 0;
                    }
                }

                return -6;
            }
        }

        return -7;
    }

    public async Task<int> AddRequestAsync(FriendEntry requester, long targetId)
    {
        var member = State.TeamEntry.Members.FirstOrDefault(m => m.AccountId == targetId);

        if (member == null)
            return -1;

        if (State.TeamEntry.Members.Any(x => x.AccountId == requester.AccountId))
            return -2;

        if (State.TeamEntry.Members.Count >= State.TeamEntry.MaxPlayers)
            return -3;

        if (State.TeamEntry.JoinRequests.Any(x => x.JoinerId == requester.AccountId))
            return -4;

        var targetHome = GrainHelper.GetHomeGrain(GrainFactory, targetId);

        var res = await targetHome.GetIsPossibleToTeamRequest(requester.AccountId, requester.Alliance?.AllianceId ?? 0);

        switch (res)
        {
            case -1: // muted
                return -5;
            case -2: // invalid alliance
                return -6;
            case -3: // no friends
                return -7;
            case -4: // target offline
                return -8;
            case -5: // invites blocked
                return -9;
        }

        State.TeamEntry.JoinRequests.Add(new TeamJoinRequest
        {
            JoinerId = requester.AccountId,
            TargetId = targetId,
            FriendEntry = requester
        });

        await SendTeamMessageToMembersAsync();
        return 0;
    }

    public async Task<int> ChangeRequestStatusAsync(long joinerId, int status)
    {
        var request = State.TeamEntry.JoinRequests.FirstOrDefault(x => x.JoinerId == joinerId);

        if (request == null)
            return -1;

        switch (status)
        {
            case 1: // cancelled
            {
                State.TeamEntry.JoinRequests.RemoveAll(x => x.JoinerId == request.JoinerId);

                foreach (var inv in State.TeamEntry.Invites.ToArray())
                {
                    if (inv.TargetId != request.JoinerId) continue;

                    _ = GrainHelper.GetHomeGrain(GrainFactory, request.JoinerId).TeamInviteCancelAsync(inv.TeamId);

                    State.TeamEntry.Invites.Remove(inv);
                }

                await SendTeamMessageToMembersAsync();
                break;
            }
            case 2: // y
            {
                State.TeamEntry.JoinRequests.RemoveAll(x => x.JoinerId == request.JoinerId);

                foreach (var inv in State.TeamEntry.Invites.ToArray())
                {
                    if (inv.TargetId != request.JoinerId) continue;

                    _ = GrainHelper.GetHomeGrain(GrainFactory, request.JoinerId).TeamInviteCancelAsync(inv.TeamId);

                    State.TeamEntry.Invites.Remove(inv);
                }

                if (State.TeamEntry.Members.All(x => x.AccountId != request.TargetId))
                {
                    var jnrh = GrainHelper.GetHomeGrain(GrainFactory, request.JoinerId);
                    _ = jnrh.RemoveTeamRequest(State.TeamEntry.TeamId, 3);

                    return -2;
                }

                if (State.TeamEntry.Members.Any(x => x.AccountId == request.JoinerId))
                {
                    var jnrh = GrainHelper.GetHomeGrain(GrainFactory, request.JoinerId);
                    _ = jnrh.RemoveTeamRequest(State.TeamEntry.TeamId, 1);

                    return -3;
                }

                var maxPlayers =
                    LogicGamePlayUtil.GetPlayerCountWithGameModeVariation(State.TeamEntry.GameModeVariation,
                        State.TeamEntry.IsFriendlyRoom);

                if (State.TeamEntry.Members.Count >= maxPlayers)
                {
                    var jnrh = GrainHelper.GetHomeGrain(GrainFactory, request.JoinerId);
                    _ = jnrh.RemoveTeamRequest(State.TeamEntry.TeamId, 2);

                    return -4;
                }

                var index = GetTeamIndexForNewPlayer();

                if (index < 0)
                {
                    var jnrh = GrainHelper.GetHomeGrain(GrainFactory, request.JoinerId);
                    _ = jnrh.RemoveTeamRequest(State.TeamEntry.TeamId, 2);

                    return -6;
                }

                var memberEntry = new TeamMemberEntry
                {
                    AccountId = request.JoinerId
                };

                TeamMemberData? d;

                try
                {
                    var jnrh = GrainHelper.GetHomeGrain(GrainFactory, request.JoinerId);

                    d = await jnrh.GetBasicTeamMemberData();

                    if (d == null)
                        return -5;

                    _ = jnrh.RemoveTeamRequest(State.TeamEntry.TeamId, 1);
                }
                catch (Exception e)
                {
                    Logger.Warn(e.ToString());
                    return -6;
                }

                memberEntry.DisplayData = d.DisplayData;

                memberEntry.CharacterGlobalId = d.CharacterGlobalId;
                memberEntry.SkinGlobalId = d.SkinGlobalId;

                memberEntry.HeroTrophies = d.HeroTrophies;
                memberEntry.HeroMaxTrophies = d.HeroMaxTrophies;
                memberEntry.HeroPowerLevel = d.HeroPowerLevel;

                memberEntry.StarPowerGlobalId = d.StarPowerGlobalId;
                memberEntry.DifficultyLevel = d.DifficultyLevel;

                memberEntry.TeamIndex = index;
                memberEntry.State = 3;

                State.TeamEntry.Members.Add(memberEntry);

                var maxPlayersInTeam =
                    LogicGamePlayUtil.GetPlayerCountInTeamWithGameModeVariation(
                        State.TeamEntry.GameModeVariation, State.TeamEntry.IsFriendlyRoom);

                if (State.TeamEntry.Members.Count(x => x.TeamIndex == index) >= maxPlayersInTeam)
                    foreach (var inv in State.TeamEntry.Invites
                                 .Where(x => x.TeamIndex == index).ToArray())
                    {
                        _ = GrainHelper.GetHomeGrain(GrainFactory, inv.TargetId).TeamInviteCancelAsync(inv.TeamId);

                        State.TeamEntry.Invites.Remove(inv);
                    }

                if (State.TeamEntry.Members.Count >= State.TeamEntry.MaxPlayers)
                {
                    foreach (var inv in State.TeamEntry.Invites.ToArray())
                    {
                        _ = GrainHelper.GetHomeGrain(GrainFactory, inv.TargetId).TeamInviteCancelAsync(inv.TeamId);

                        State.TeamEntry.Invites.Remove(inv);
                    }

                    foreach (var req in State.TeamEntry.JoinRequests.ToArray())
                    {
                        _ = GrainHelper.GetHomeGrain(GrainFactory, req.JoinerId)
                            .RemoveTeamRequest(State.TeamEntry.TeamId, 2);

                        State.TeamEntry.JoinRequests.Remove(req);
                    }
                }

                await SendTeamMessageToMembersAsync();

                await SendEventStreamEntryAsync(memberEntry.AccountId, memberEntry.DisplayData.AvatarName, 102);

                await UpdateAllianceTeamInAllAlliancesAsync();
                await UpdateAllianceTeamInAllFriendshipsAsync();

                _ = StopPlayerSearchAsync();
                _ = CancelMatchmakingAsync();

                return 0;
            }
            case 3: // n
            {
                State.TeamEntry.JoinRequests.RemoveAll(x => x.JoinerId == request.JoinerId);

                foreach (var inv in State.TeamEntry.Invites.ToArray())
                {
                    if (inv.TargetId != request.JoinerId) continue;

                    _ = GrainHelper.GetHomeGrain(GrainFactory, request.JoinerId).TeamInviteCancelAsync(inv.TeamId);

                    State.TeamEntry.Invites.Remove(inv);
                }

                var jnrh = GrainHelper.GetHomeGrain(GrainFactory, request.JoinerId);
                _ = jnrh.RemoveTeamRequest(State.TeamEntry.TeamId, 0);

                await SendTeamMessageToMembersAsync();
                return 0;
            }
        }

        return -7;
    }

    public async Task RemoveAllPlayerPendingActionsAsync(long accountId)
    {
        foreach (var inv in State.TeamEntry.Invites.Where(inv => inv.TargetId == accountId))
            await GrainHelper.GetPlayerSession(GrainFactory, inv.InviterId).SendMessagesToPlayerAsync([
                LaserContractSerializer.SerializeToStruct(new TeamErrorMessage { ErrorCode = 44 })
            ]);

        State.TeamEntry.Invites.RemoveAll(x => x.TargetId == accountId);
        State.TeamEntry.JoinRequests.RemoveAll(x => x.JoinerId == accountId);

        await SendTeamMessageToMembersAsync();
    }

    public Task<long[]> SyncWithAllianceAsync(long allianceId, long[] allianceMembers)
    {
        if (State.TeamEntry.Members.Any(x => allianceMembers.Contains(x.AccountId)))
            return Task.FromResult(State.TeamEntry.Members.Select(x => x.AccountId).ToArray());

        State.MemberAlliances = State.MemberAlliances
            .Where(x => x.Value != allianceId)
            .ToDictionary();

        return Task.FromResult(State.TeamEntry.Members.Select(x => x.AccountId).ToArray());
    }

    public async Task<int> AddOrChangeMemberAllianceIdAsync(long memberId, long allianceId)
    {
        var member = State.TeamEntry.Members.FirstOrDefault(x => x.AccountId == memberId);

        if (member == null)
            return -1;

        if (allianceId <= 0)
            return await RemoveMemberAllianceIdAsync(memberId);

        State.MemberAlliances[memberId] = allianceId;

        await UpdateAllianceTeamInAllAlliancesAsync();
        return 0;
    }

    public ValueTask<AllianceTeamEntry> GetAllianceTeamEntry()
    {
        return ValueTask.FromResult(new AllianceTeamEntry
        {
            RoomType = State.TeamEntry.RoomType,
            TeamId = State.TeamEntry.TeamId,
            OwnerAccountId = State.TeamEntry.Members.FirstOrDefault(x => x.IsOwner)?.AccountId ?? 0,
            MaxPlayers = State.TeamEntry.MaxPlayers,
            Players = State.TeamEntry.Members.Select(x => x.AccountId).ToArray()
        });
    }

    public ValueTask<bool> IAmSolo(long memberId)
    {
        return ValueTask.FromResult(State.TeamEntry.Members.Count == 1 &&
                                    State.TeamEntry.Members.First().AccountId == memberId);
    }

    public async Task<int> StartPlayersSearchAsync(long memberId, int memberRegionGlobalId, int myTrophies)
    {
        var member = State.TeamEntry.Members.FirstOrDefault(x => x.AccountId == memberId);

        if (member == null)
            return -1;

        if (!member.IsOwner)
            return -2;

        if (member.IsReady)
            return -3;

        if (IsPlayersSearchStarted())
            return -4;

        if (State.TeamEntry.Members.Count >= State.TeamEntry.MaxPlayers)
            return -5;

        if (State.TeamEntry.IsFriendlyRoom)
            return -6;

        await StartPlayerSearchAsync(memberId, memberRegionGlobalId, myTrophies);
        return 0;
    }

    public async Task<int> StopPlayersSearchAsync(long memberId)
    {
        var member = State.TeamEntry.Members.FirstOrDefault(x => x.AccountId == memberId);

        if (member == null)
            return -1;

        if (!member.IsOwner)
            return -2;

        if (member.IsReady)
            return -3;

        if (!IsPlayersSearchStarted())
            return -4;

        if (State.TeamEntry.IsFriendlyRoom)
            return -5;

        await StopPlayerSearchAsync();
        _ = CancelMatchmakingAsync();

        return 0;
    }

    public async Task<int> JoinByTeamPlayersSearchAsync(TeamMemberEntry memberEntry)
    {
        if (!IsPlayersSearchStarted())
            return -1;

        return await JoinByIdAsync(memberEntry);
    }

    public async Task ToBattleFromMatchmakingAsync(Guid id)
    {
        if (!_matchmakingStarted)
            return;

        MatchmakingServiceGrain = null;
        MatchmakeId = null;

        foreach (var m in State.TeamEntry.Members)
            m.IsReady = false;

        await SendTeamMessageToMembersAsync();

        _matchmakingStarted = false;
    }

    private async Task TickAsync()
    {
        await CorrectMembersAsync();
        await Task.WhenAll(CorrectInvitesAsync(), CorrectRequestsAsync(), CorrectLocationAsync());

        if (State.TeamEntry.Members.Count >= State.TeamEntry.MaxPlayers)
            _ = StopPlayerSearchAsync();

        await CorrectMatchmakingAsync();

        await SendTeamMessageToMembersAsync();
    }

    private async Task CorrectMatchmakingAsync()
    {
        try
        {
            if (MatchmakeId != null && MatchmakingServiceGrain != null)
            {
                var ms = State.TeamEntry.Members.Select(x => x.AccountId).ToList();

                var res = await MatchmakingServiceGrain.IsContainsPlayersInMatchmakingAsync(MatchmakeId.Value, ms);

                if (res != 1)
                    await CancelMatchmakingAsync();
            }
        }
        catch (Exception e)
        {
            Logger.Warn(e.ToString());
        }
    }

    private async Task CorrectMembersAsync()
    {
        foreach (var m in State.TeamEntry.Members.ToArray())
            try
            {
                var h = GrainHelper.GetHomeGrain(GrainFactory, m.AccountId);
                if (await h.GetTeamId() == State.TeamEntry.TeamId) continue;

                await LeaveAsync(m.AccountId);
            }
            catch (Exception e)
            {
                Logger.Warn(e.ToString());
                _ = LeaveAsync(m.AccountId);
            }
    }

    private async Task CorrectInvitesAsync()
    {
        foreach (var inv in State.TeamEntry.Invites.ToArray())
            try
            {
                var tg = GrainHelper.GetHomeGrain(GrainFactory, inv.TargetId);

                if (State.TeamEntry.Members.Any(x => x.AccountId == inv.TargetId))
                {
                    State.TeamEntry.Invites.Remove(inv);
                    continue;
                }

                if (State.TeamEntry.Members.All(x => x.AccountId != inv.InviterId))
                {
                    State.TeamEntry.Invites.Remove(inv);

                    _ = tg.TeamInviteCancelAsync(inv.TeamId);

                    continue;
                }

                var t = await GrainHelper.GetHomeGrain(GrainFactory, inv.InviterId).GetLastKeepAliveReceivedTime();

                if ((DateTime.UtcNow - t).TotalSeconds > 35)
                {
                    State.TeamEntry.Invites.Remove(inv);

                    _ = tg.TeamInviteCancelAsync(inv.TeamId);

                    continue;
                }

                var t2 = await tg.GetLastKeepAliveReceivedTime();

                // ReSharper disable once InvertIf
                if ((DateTime.UtcNow - t2).TotalSeconds > 35)
                {
                    inv.Status = 3;

                    State.TeamEntry.Invites.Remove(inv);

                    await GrainHelper.GetPlayerSession(GrainFactory, inv.InviterId).SendMessagesToPlayerAsync([
                        LaserContractSerializer.SerializeToStruct(new TeamErrorMessage { ErrorCode = 44 })
                    ]);

                    continue;
                }

                var tid = await tg.GetTeamId();

                if (tid <= 0) continue;

                State.TeamEntry.Invites.Remove(inv);

                await GrainHelper.GetPlayerSession(GrainFactory, inv.InviterId).SendMessagesToPlayerAsync([
                    LaserContractSerializer.SerializeToStruct(new TeamErrorMessage { ErrorCode = 29 })
                ]);
            }
            catch (Exception e)
            {
                Logger.Warn(e.ToString());
                State.TeamEntry.Invites.Remove(inv);
            }
    }

    private async Task CorrectRequestsAsync()
    {
        foreach (var req in State.TeamEntry.JoinRequests.ToArray())
            try
            {
                if (State.TeamEntry.Members.All(x => x.AccountId != req.TargetId))
                {
                    State.TeamEntry.JoinRequests.Remove(req);

                    _ = GrainHelper.GetHomeGrain(GrainFactory, req.JoinerId)
                        .RemoveTeamRequest(State.TeamEntry.TeamId, 3);

                    continue;
                }

                if (State.TeamEntry.Members.Any(x => x.AccountId == req.JoinerId))
                {
                    State.TeamEntry.JoinRequests.Remove(req);
                    continue;
                }

                var t = await GrainHelper.GetHomeGrain(GrainFactory, req.TargetId).GetLastKeepAliveReceivedTime();

                if ((DateTime.UtcNow - t).TotalSeconds > 35)
                {
                    State.TeamEntry.JoinRequests.Remove(req);

                    _ = GrainHelper.GetHomeGrain(GrainFactory, req.JoinerId)
                        .RemoveTeamRequest(State.TeamEntry.TeamId, 4);

                    continue;
                }

                var jnr = GrainHelper.GetHomeGrain(GrainFactory, req.JoinerId);

                var t2 = await jnr.GetLastKeepAliveReceivedTime();

                // ReSharper disable once InvertIf
                if ((DateTime.UtcNow - t2).TotalSeconds > 35)
                {
                    State.TeamEntry.JoinRequests.Remove(req);

                    _ = jnr.RemoveTeamRequest(State.TeamEntry.TeamId, 5);
                    continue;
                }

                var tid = await jnr.GetTeamId();

                if (tid <= 0) continue;

                State.TeamEntry.JoinRequests.Remove(req);

                _ = jnr.RemoveTeamRequest(State.TeamEntry.TeamId, 5);
            }
            catch (Exception e)
            {
                Logger.Warn(e.ToString());
                State.TeamEntry.JoinRequests.Remove(req);
            }
    }

    private async Task CorrectLocationAsync()
    {
        if (State.TeamEntry.IsFriendlyRoom) return;

        var events = EventsManager.GetEvents().ToArray();

        var eventData = events.FirstOrDefault(x => x.SlotId == State.TeamEntry.EventSlot);

        if (eventData == null)
            return;

        var location = LogicDataTables.GetDataById<LogicLocationData>(eventData.LocationGlobalId);

        if (location == null)
            return;

        if (State.TeamEntry.LocationGlobalId == location.GlobalId)
            return;

        _ = StopPlayerSearchAsync();
        _ = CancelMatchmakingAsync();

        await SetEventAsync(State.TeamEntry.EventSlot,
            State.TeamEntry.Members.FirstOrDefault(x => x.IsOwner)?.AccountId ?? 0);
    }

    private async Task DeleteTeamAsync()
    {
        await RemoveAllianceTeamInAllAlliancesAsync();
        await RemoveAllianceTeamInAllFriendshipsAsync();

        _ = StopPlayerSearchAsync();
        _ = CancelMatchmakingAsync();

        State.TeamCreatedTime = null;

        State.TeamEntry = new TeamEntry();

        State.StreamEntries.Clear();
        State.KickedPlayers.Clear();
        State.MemberAlliances.Clear();

        _tickTimer?.Dispose();
        _tickTimer = null;
    }

    private async Task SendStreamEntryAsync(StreamEntry streamEntry, bool changeStreamEntryId = true)
    {
        if (changeStreamEntryId)
            streamEntry.StreamEntryId = Random.Shared.NextInt64();

        streamEntry.SendTime = DateTime.UtcNow;

        State.StreamEntries.Add(streamEntry);

        var message = new TeamStreamMessage
        {
            TeamId = State.TeamEntry.TeamId,
            StreamEntries = [streamEntry]
        };

        var ms = LaserContractSerializer.SerializeToStruct(message);

        if (State.StreamEntries.Count > TeamSettings.GetConfig().MaxMessagesInChat)
        {
            var fistStreamEntry = State.StreamEntries.First();

            State.StreamEntries.Remove(fistStreamEntry);

            var message2 = new TeamStreamEntryRemovedMessage
            {
                TeamId = State.TeamEntry.TeamId,
                StreamId = fistStreamEntry.StreamEntryId
            };

            var ms2 = LaserContractSerializer.SerializeToStruct(message2);

            await SendMessagesToMembersAsync([ms, ms2]);
            return;
        }

        await SendMessagesToMembersAsync([ms]);
    }

    private async ValueTask SendMessagesToMembersAsync(PiranhaMessageStruct[] m)
    {
        foreach (var me in State.TeamEntry.Members)
            await GrainHelper.GetPlayerSession(GrainFactory, me.AccountId)
                .SendMessagesToPlayerAsync(m);
    }

    private async ValueTask SendTeamMessageToMembersAsync()
    {
        await SendMessagesToMembersAsync([
            LaserContractSerializer.SerializeToStruct(new TeamMessage { TeamEntry = State.TeamEntry })
        ]);
    }

    private void UpdateTeamMemberIndexes()
    {
        var members = State.TeamEntry.Members.ToArray();

        switch (State.TeamEntry.GameModeVariation)
        {
            case 6:
            case 14:
            case 15:
                if (State.TeamEntry.LastMemberIndexesType == 1)
                    return;

                foreach (var m in members)
                    m.TeamIndex = 0;

                State.TeamEntry.LastMemberIndexesType = 1;
                break;

            case 0:
            case 2:
            case 3:
            case 5:
            case 11:
            case 16:
                if (State.TeamEntry.LastMemberIndexesType == 2)
                    return;

                for (var i = 0; i < members.Length; i++)
                    members[i].TeamIndex = i < 3 ? 0 : 1;

                State.TeamEntry.LastMemberIndexesType = 2;
                break;

            case 9:
                if (State.TeamEntry.LastMemberIndexesType == 3)
                    return;

                for (var i = 0; i < members.Length; i++)
                    members[i].TeamIndex = i / 2;

                State.TeamEntry.LastMemberIndexesType = 3;
                break;

            case 7:
                if (State.TeamEntry.LastMemberIndexesType == 4)
                    return;

                for (var i = 0; i < members.Length; i++)
                    members[i].TeamIndex = i == 0 ? 0 : 1;

                State.TeamEntry.LastMemberIndexesType = 4;
                break;

            case 8:
            case 10:
                if (State.TeamEntry.LastMemberIndexesType == 5)
                    return;

                foreach (var m in members)
                    m.TeamIndex = 0;

                State.TeamEntry.LastMemberIndexesType = 5;
                break;

            case 13:
                if (State.TeamEntry.LastMemberIndexesType == 6)
                    return;

                if (members.Length > 0)
                    members[0].TeamIndex = 0;

                State.TeamEntry.LastMemberIndexesType = 6;
                break;

            default:
                throw new ArgumentException($"Unknown game mode variation: {State.TeamEntry.GameModeVariation}");
        }
    }

    private int GetTeamIndexForNewPlayer()
    {
        var members = State.TeamEntry.Members.ToArray();
        var invites = State.TeamEntry.Invites.ToArray();

        var maxPlayersInTeam = LogicGamePlayUtil.GetPlayerCountInTeamWithGameModeVariation(
            State.TeamEntry.GameModeVariation, State.TeamEntry.IsFriendlyRoom);

        switch (State.TeamEntry.GameModeVariation)
        {
            case 6:
            case 8:
            case 10:
            case 13:
            case 14:
            case 15:
                if (members.Length + invites.Length >= maxPlayersInTeam)
                    return -1;

                return 0;

            case 0:
            case 2:
            case 3:
            case 5:
            case 11:
            case 16:
                var count0 = members.Count(m => m.TeamIndex == 0);
                var count1 = members.Count(m => m.TeamIndex == 1);

                var count2 = invites.Count(i => i.TeamIndex == 0);
                var count3 = invites.Count(i => i.TeamIndex == 1);

                // ReSharper disable once InvertIf
                if (count0 == count1)
                    if (count0 >= maxPlayersInTeam)
                        return -1;

                if (count0 + count2 < maxPlayersInTeam)
                    return 0;

                if (count1 + count3 < maxPlayersInTeam)
                    return 1;

                return -1;

            case 9:
                for (var team = 0; team < 5; team++)
                    if (members.Count(m => m.TeamIndex == team) +
                        invites.Count(i => i.TeamIndex == team) < maxPlayersInTeam)
                        return team;

                return -1;

            case 7:
                var count5 = members.Count(m => m.TeamIndex == 1) + invites.Count(i => i.TeamIndex == 1);

                return members.Any(m => m.TeamIndex == 0) ? count5 < maxPlayersInTeam ? 1 : -1 : 0;

            default:
                throw new ArgumentException($"Unknown game mode variation: {State.TeamEntry.GameModeVariation}");
        }
    }

    private async Task<int> StartMatchmakingAsync(string region)
    {
        if (_matchmakingStarted)
            return -1;

        var member = State.TeamEntry.Members.FirstOrDefault(m => m.IsOwner);

        if (member == null)
        {
            foreach (var m in State.TeamEntry.Members)
                m.IsReady = false;

            await SendTeamMessageToMembersAsync();
            return -2;
        }

        if (State.TeamEntry.Invites.Count > 0)
        {
            foreach (var m in State.TeamEntry.Members)
                m.IsReady = false;

            await SendTeamMessageToMembersAsync();
            return -3;
        }

        if (State.TeamEntry.IsFriendlyRoom)
        {
            foreach (var m in State.TeamEntry.Members)
                m.IsReady = false;

            var gameMode = State.TeamEntry.GameModeVariation;
            var locationId = State.TeamEntry.LocationGlobalId;
            var plrs = State.TeamEntry.Members.Select(x => (x.AccountId, State.TeamEntry.TeamId)).ToDictionary();
            var teams = State.TeamEntry.Members.Select(x => (x.AccountId, x.TeamIndex)).ToDictionary();
            var accId = member.AccountId;
            var avatarName = member.DisplayData.AvatarName;
            int[] mods = [];

            switch (gameMode)
            {
                case 6:
                case 8:
                case 10:
                case 13:
                case 14:
                case 15:
                {
                    var keys = teams.Keys.ToList();

                    for (var i = 0; i < keys.Count; i++)
                        teams[keys[i]] = i;

                    break;
                }
            }

            this.RegisterGrainTimer(async _ =>
            {
                try
                {
                    var b = GrainHelper.GetBattleGrain(GrainFactory, Guid.NewGuid());

                    var res = await b.CreateBattleAsync(gameMode, locationId, mods, 0, true, plrs, teams);

                    if (res == 0)
                    {
                        await SendEventStreamEntryAsync(accId, avatarName, 110);
                        return;
                    }

                    Logger.Warn(
                        $"Error while creating battle: {res}! {locationId}_{string.Join(", ", mods)}" +
                        $"_{0}_{plrs.Count}");

                    await SendEventStreamEntryAsync(accId, avatarName, 111);
                    await SendTeamMessageToMembersAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Async battle creation crashed: {ex}");
                }
            }, new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromMilliseconds(1),
                Period = Timeout.InfiniteTimeSpan,
                Interleave = true
            });

            return 0;
        }

        await SendEventStreamEntryAsync(member.AccountId, member.DisplayData.AvatarName, 105);

        try
        {
            await SendMessagesToMembersAsync([
                LaserContractSerializer.SerializeToStruct(new TeamGameStartingMessage
                    { LocationGlobalId = State.TeamEntry.LocationGlobalId })
            ]);

            var tr = await CalculateAvgHeroTrophiesAsync();
            var sector = LogicGameModeUtil.GetMmSectorByTrophies(tr);
            var ml = State.TeamEntry.Members.Select(x => (x.AccountId, State.TeamEntry.TeamId)).ToList();

            var mm = GrainHelper.GetMatchmakingGrain(GrainFactory, State.TeamEntry.EventSlot, sector, region.ToLower());
            var r = await mm.AddPlayersToMatchmakingAsync(ml, tr, region.ToLower());

            switch (r.Item1)
            {
                case -1 or -2 or -3 or -4 or -5:
                {
                    foreach (var m in State.TeamEntry.Members)
                        m.IsReady = false;

                    await SendMessagesToMembersAsync([
                        LaserContractSerializer.SerializeToStruct(new MatchMakingCancelledMessage()),
                        LaserContractSerializer.SerializeToStruct(new TeamErrorMessage { ErrorCode = 1 }),
                        LaserContractSerializer.SerializeToStruct(new TeamMessage { TeamEntry = State.TeamEntry })
                    ]);

                    return -4;
                }
            }

            MatchmakingServiceGrain = mm;
            MatchmakeId = r.Item2;
        }
        catch
        {
            foreach (var m in State.TeamEntry.Members)
                m.IsReady = false;

            await SendMessagesToMembersAsync([
                LaserContractSerializer.SerializeToStruct(new MatchMakingCancelledMessage()),
                LaserContractSerializer.SerializeToStruct(new TeamErrorMessage { ErrorCode = 1 }),
                LaserContractSerializer.SerializeToStruct(new TeamMessage { TeamEntry = State.TeamEntry })
            ]);

            return -5;
        }

        _matchmakingStarted = true;

        return 0;
    }

    private async Task<int> SetMemberStatusAsync(long memberId, int status)
    {
        var member = State.TeamEntry.Members.FirstOrDefault(m => m.AccountId == memberId);

        if (member == null)
            return -1;

        member.State = status;

        if (status > 0)
        {
            await SendTeamMessageToMembersAsync();
            return 0;
        }

        member.IsReady = false;

        if (_matchmakingStarted)
            await SendMessagesToMembersAsync([
                LaserContractSerializer.SerializeToStruct(new MatchmakeFailedMessage { ErrorCode = 11 })
            ]);

        _ = CancelMatchmakingAsync();

        if (member.IsOwner)
            _ = StopPlayerSearchAsync();

        foreach (var inv in State.TeamEntry.Invites.ToArray())
        {
            if (inv.InviterId != memberId)
                continue;

            State.TeamEntry.Invites.Remove(inv);

            _ = GrainHelper.GetHomeGrain(GrainFactory, inv.TargetId).TeamInviteCancelAsync(inv.TeamId);
        }

        foreach (var req in State.TeamEntry.JoinRequests.ToArray())
        {
            if (req.TargetId != memberId)
                continue;

            State.TeamEntry.JoinRequests.Remove(req);

            _ = GrainHelper.GetHomeGrain(GrainFactory, req.JoinerId).RemoveTeamRequest(State.TeamEntry.TeamId, 4);
        }

        await SendTeamMessageToMembersAsync();
        return 0;
    }

    private int CheckInviteTeamIndex(int teamIndex)
    {
        var c = LogicGamePlayUtil.GetPlayerCountInTeamWithGameModeVariation(State.TeamEntry.GameModeVariation,
            State.TeamEntry.IsFriendlyRoom);

        switch (State.TeamEntry.GameModeVariation)
        {
            case 6:
            case 8:
            case 10:
            case 13:
            case 14:
            case 15:
                if (teamIndex != 0) return -12;

                if (State.TeamEntry.Members.Count(x => x.TeamIndex == 0) +
                    State.TeamEntry.Invites.Count(x => x.TeamIndex == 0) >= c)
                    return -12;

                break;

            case 0:
            case 2:
            case 3:
            case 5:
            case 11:
            case 16:
                switch (teamIndex)
                {
                    case 0:
                    {
                        if (State.TeamEntry.Members.Count(x => x.TeamIndex == 0) +
                            State.TeamEntry.Invites.Count(x => x.TeamIndex == 0) >= c)
                            return -13;

                        break;
                    }
                    case 1:
                    {
                        if (State.TeamEntry.Members.Count(x => x.TeamIndex == 1) +
                            State.TeamEntry.Invites.Count(x => x.TeamIndex == 1) >= c)
                            return -14;

                        break;
                    }
                    default:
                        return -15;
                }

                break;

            case 9:
                if (teamIndex is < 0 or > 4)
                    return -16;

                if (State.TeamEntry.Members.Count(x => x.TeamIndex == teamIndex) +
                    State.TeamEntry.Invites.Count(x => x.TeamIndex == teamIndex) >= c)
                    return -17;

                break;

            case 7:
                switch (teamIndex)
                {
                    case 0:
                    {
                        if (State.TeamEntry.Members.Count(x => x.TeamIndex == 0) +
                            State.TeamEntry.Invites.Count(x => x.TeamIndex == 0) >= c)
                            return -18;

                        break;
                    }
                    case 1:
                    {
                        if (State.TeamEntry.Members.Count(x => x.TeamIndex == 1) +
                            State.TeamEntry.Invites.Count(x => x.TeamIndex == 1) >= c)
                            return -19;

                        break;
                    }
                    default:
                        return -20;
                }

                break;

            default:
                throw new ArgumentException($"Unknown game mode variation: {State.TeamEntry.GameModeVariation}");
        }

        return 0;
    }

    private async Task<int> RemoveMemberAllianceIdAsync(long memberId)
    {
        var r = State.MemberAlliances.Remove(memberId, out var value);

        if (!r)
            return -1;

        await UpdateAllianceTeamInAllAlliancesAsync();
        await RemoveAllianceTeamInAllianceAsync(value);

        return 0;
    }

    private async Task UpdateAllianceTeamInAllAlliancesAsync()
    {
        var e = await GetAllianceTeamEntry();

        foreach (var allianceId in State.MemberAlliances.Values.Distinct())
        {
            var alliance = GrainHelper.GetAllianceGrain(GrainFactory, allianceId);
            await alliance.AddOrUpdateTeamAsync(e);
        }
    }

    private async Task RemoveAllianceTeamInAllianceAsync(long allianceId)
    {
        var alliance = GrainHelper.GetAllianceGrain(GrainFactory, allianceId);
        await alliance.RemoveTeamAsync(State.TeamEntry.TeamId);
    }

    private async Task RemoveAllianceTeamInAllAlliancesAsync()
    {
        foreach (var allianceId in State.MemberAlliances.Values.Distinct())
        {
            var alliance = GrainHelper.GetAllianceGrain(GrainFactory, allianceId);
            await alliance.RemoveTeamAsync(State.TeamEntry.TeamId);
        }
    }

    private async Task UpdateAllianceTeamInAllFriendshipsAsync()
    {
        var e = await GetAllianceTeamEntry();

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var member in State.TeamEntry.Members)
        {
            var home = GrainHelper.GetHomeGrain(GrainFactory, member.AccountId);
            await home.UpdateAllianceTeamEntryInFriendshipAsync(e);
        }
    }

    private async Task RemoveAllianceTeamInFriendshipAsync(long accountId)
    {
        var home = GrainHelper.GetHomeGrain(GrainFactory, accountId);
        await home.UpdateAllianceTeamEntryInFriendshipAsync(null);
    }

    private async Task RemoveAllianceTeamInAllFriendshipsAsync()
    {
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var member in State.TeamEntry.Members)
        {
            var home = GrainHelper.GetHomeGrain(GrainFactory, member.AccountId);
            await home.UpdateAllianceTeamEntryInFriendshipAsync(null);
        }
    }

    private bool IsPlayersSearchStarted()
    {
        return _playersSearchBucket != null && _playersSearchTeamKey != null;
    }

    private async Task StartPlayerSearchAsync(long memberId, int memberRegionGlobalId, int myTrophies)
    {
        var v1 = $"players_search:{State.TeamEntry.LocationGlobalId}";
        var v2 = $"{memberRegionGlobalId}:{State.TeamEntry.TeamId}";

        var avgCups = await CalculateAvgTrophiesAsync(memberId, myTrophies);
        await teamPlayersSearchService.UpdateTeamInSearch(v1, v2, avgCups);

        _playersSearchBucket = v1;
        _playersSearchTeamKey = v2;
    }

    private async Task StopPlayerSearchAsync()
    {
        if (!IsPlayersSearchStarted())
            return;

        var owner = State.TeamEntry.Members.FirstOrDefault(x => x.IsOwner);

        if (owner != null)
        {
            var home = GrainHelper.GetHomeGrain(GrainFactory, owner.AccountId);
            await home.StopPlayersSearchAsync();
        }

        await teamPlayersSearchService.RemoveTeamFromSearch(_playersSearchBucket!, _playersSearchTeamKey!);

        _playersSearchBucket = null;
        _playersSearchTeamKey = null;
        _lastPlayersSearchEndTime = DateTime.UtcNow;
    }

    private bool IsPlayerSearchBug1WhenTeamIsFull()
    {
        if ((DateTime.UtcNow - _lastPlayersSearchEndTime) >= TimeSpan.FromSeconds(2))
            return false;

        var maxPlayers = LogicGamePlayUtil.GetPlayerCountWithGameModeVariation(State.TeamEntry.GameModeVariation,
            State.TeamEntry.IsFriendlyRoom);

        return State.TeamEntry.Members.Count >= maxPlayers;
    }

    private async Task<int> CalculateAvgTrophiesAsync(long memberId, int trophies)
    {
        try
        {
            if (State.TeamEntry.Members.Count == 0)
                return 0;

            var tasks = State.TeamEntry.Members
                .ToArray()
                .Where(x => x.AccountId != memberId)
                .Select(member => GrainHelper.GetHomeGrain(GrainFactory, member.AccountId).GetNowTrophies()
                );

            var results = await Task.WhenAll(tasks);

            return (results.Sum() + trophies) / (results.Length + 1);
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to calculate avg trophies");
            return 0;
        }
    }

    private async Task<int> CalculateAvgHeroTrophiesAsync()
    {
        try
        {
            if (State.TeamEntry.Members.Count == 0)
                return 0;

            return State.TeamEntry.Members.Sum(x => x.HeroTrophies) / State.TeamEntry.Members.Count;
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to calculate avg hero trophies");
            return 0;
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await DeleteTeamAsync();
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var teamId = this.GetPrimaryKeyString().Split('_').Last();

        State.TeamEntry.TeamId = Convert.ToInt64(teamId);
    }
}