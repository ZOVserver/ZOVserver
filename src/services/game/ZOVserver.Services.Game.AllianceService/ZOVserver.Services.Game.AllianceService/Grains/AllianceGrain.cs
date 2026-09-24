using MessagePack;
using NLog;
using Orleans.Providers;
using ZOVserver.Services.Game.AllianceService.Leaderboard;
using ZOVserver.Services.Game.AllianceService.States;
using ZOVserver.Services.Shared.AllianceSearchService;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Interfaces;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;
using ZOVserver.Shared.Contracts.Laser.Combined.Stream;
using ZOVserver.Shared.Contracts.Laser.Combined.Stream.Inheritors;
using ZOVserver.Shared.Contracts.Laser.Combined.Stream.Inheritors.Target;
using ZOVserver.Shared.Contracts.Laser.Machine;
using ZOVserver.Shared.Contracts.Laser.Messages.Server;
using ZOVserver.Shared.Contracts.Models;
using ZOVserver.Shared.Contracts.Structs;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Services.Game.AllianceService.Grains;

[StorageProvider(ProviderName = "MongoStorage")]
// ReSharper disable once UnusedType.Global
public class AllianceGrain(AllianceOpenSearchWorker openSearchWorker) : Grain<AllianceState>, IAllianceServiceGrain
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<long, DateTime> _elderKicks = [];

    private readonly List<StreamEntry> _streamEntries = [];
    private readonly Dictionary<long, AllianceTeamEntry> _teamEntries = [];

    private IDisposable? _tickTimer;

    public async Task<AllianceParams?> CreateAllianceAsync(AllianceParams startParams, AllianceMember owner)
    {
        if (State.AllianceId != startParams.AllianceId)
            return null;

        if (owner.Role != AllianceRole.Leader)
            return null;

        if (owner.AccountId != startParams.OwnerAccountId)
            return null;

        if (State.CreatedDateTime != null)
            return null;

        State.OwnerAccountId = owner.AccountId;
        State.CreatedDateTime = DateTime.UtcNow;

        State.Name = startParams.Name;
        State.Description = startParams.Description;

        State.RegionGlobalId = startParams.RegionGlobalId;
        State.LanguageGlobalId = startParams.LanguageGlobalId;
        State.BadgeGlobalId = startParams.BadgeGlobalId;

        State.CalculatedSumTrophies = 0;
        State.RequiredTrophies = startParams.RequiredTrophies;

        State.AllianceType = startParams.AllianceType;

        State.Members.Add(owner.AccountId, owner);

        await SaveAllianceAsync(true);

        _tickTimer ??= this.RegisterGrainTimer<object?>(
            async _ => await TickAsync(),
            null,
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromSeconds(50),
                Period = TimeSpan.FromMinutes(5),
                Interleave = false
            });

        return startParams;
    }

    public async Task UpdateAllianceAsync(AllianceParams newParams)
    {
        State.Name = newParams.Name;
        State.Description = newParams.Description;

        State.RegionGlobalId = newParams.RegionGlobalId;
        State.LanguageGlobalId = newParams.LanguageGlobalId;
        State.BadgeGlobalId = newParams.BadgeGlobalId;

        State.RequiredTrophies = newParams.RequiredTrophies;

        await SaveAllianceAsync();
    }

    public async Task<int> ChangeAllianceSettingsAsync(long accountId, AllianceSettings settings)
    {
        if (!State.Members.TryGetValue(accountId, out var member))
            return -1;

        if (member.Role is not (AllianceRole.Leader or AllianceRole.CoLeader))
            return -2;

        var oldRegion = LogicDataTables.GetDataById<LogicRegionData>(State.RegionGlobalId);
        var newRegion = LogicDataTables.GetDataById<LogicRegionData>(settings.RegionGlobalId);

        try
        {
            await LeaderboardContainer.LeaderboardService.UpdateAllianceRegionAsync(State.AllianceId,
                oldRegion?.Name,
                newRegion?.Name, State.CalculatedSumTrophies);
        }
        catch (Exception e)
        {
            Logger.Error(e, "Change settings failed");
            return -3;
        }

        State.Description = settings.Description;
        State.BadgeGlobalId = settings.BadgeGlobalId;
        State.RegionGlobalId = settings.RegionGlobalId;
        State.AllianceType = settings.AllianceType;
        State.RequiredTrophies = settings.RequiredTrophies;

        await SaveAllianceAsync();
        return 0;
    }

    public Task<AllianceParams> GetAllianceInfoAsync()
    {
        var allianceParams = new AllianceParams
        {
            AllianceId = State.AllianceId,
            Name = State.Name,
            Description = State.Description,
            OwnerAccountId = State.OwnerAccountId,
            AllianceType = State.AllianceType,
            RegionGlobalId = State.RegionGlobalId,
            LanguageGlobalId = State.LanguageGlobalId,
            BadgeGlobalId = State.BadgeGlobalId,
            RequiredTrophies = State.RequiredTrophies
        };

        return Task.FromResult(allianceParams);
    }

    public async Task<((AllianceParams, int, int), int?)> GetAllianceInfoAndRoleAsync(long accountId)
    {
        var allianceInfo = await GetAllianceInfoAsync();

        int? role = null;
        if (State.Members.TryGetValue(accountId, out var member))
            role = (int?)member.Role;

        return ((allianceInfo, State.Members.Count, State.CalculatedSumTrophies), role);
    }

    public async Task<DetailedAllianceModel> GetDetailedAllianceInfoAsync()
    {
        var members = State.Members.Values.ToArray();

        DetailedAllianceMemberModel[]? detailedMembers;

        if (members.Length > 0)
            detailedMembers = await FetchDetailedMembersAsync(members);
        else
            detailedMembers = [];

        var allianceParams = await GetAllianceInfoAsync();
        var teamEntries = await GetActualTeamsAsync();

        return new DetailedAllianceModel
        {
            AllianceParams = allianceParams,
            AllianceMembers = detailedMembers,
            TeamEntries = teamEntries
        };
    }

    public async Task<int> JoinMemberAsync(AllianceMember member, DetailedAllianceMemberHomeModel model,
        long? actionerId = null, string? actionerName = null)
    {
        if (State.CreatedDateTime == null)
            return -100;

        if (State.Members.Count == 0)
            return -101;

        if (member.Role != AllianceRole.Member)
            return -1;

        switch (State.AllianceType)
        {
            case 3:
                return -2;
            case 2 when actionerId == null:
                return -3;
        }

        if (model.Trophies < State.RequiredTrophies)
            return -4;

        if (State.Members.Count >= openSearchWorker.MaxMembersInAlliance)
            return -5;

        if (State.BannedMembers.TryGetValue(member.AccountId, out var time))
            if (DateTime.UtcNow < time)
                return -6;

        if (!State.Members.TryAdd(member.AccountId, member)) return -7;

        member.JoinTime = DateTime.UtcNow;

        foreach (var se in _streamEntries.ToArray())
        {
            if (se.GetStreamEntryType() != 3) continue;
            if (se is not JoinRequestAllianceStreamEntry j) continue;
            if (j.AuthorId != member.AccountId) continue;

            await RemoveStreamEntryAsync(j.StreamEntryId);
        }

        await ChangeMemberInfoAsync(member.AccountId, model);

        if (actionerId == null)
            await SendEventStreamEntryAsync(member.AccountId, model.DisplayData.AvatarName, 3);
        else
            await SendEventStreamEntryAsync(actionerId.Value, actionerName!, 2,
                new EventStreamTargetEntry
                    { AccountId = member.AccountId, Name = model.DisplayData.AvatarName });

        await SaveAllianceAsync();
        return 0;
    }

    public async Task<int> LeaveMemberAsync(long accountId, string name)
    {
        if (!State.Members.Remove(accountId, out var member))
            return -1;

        if (member.Role == AllianceRole.Leader)
        {
            var newLeader = GetMostLikelyNextPresidentId();

            if (newLeader == -1)
            {
                State.Members.Clear();
                State.BannedMembers.Clear();

                await SaveAllianceAsync();
                return 0;
            }

            State.Members[newLeader].Role = AllianceRole.Leader;

            try
            {
                var newLeaderHome = GrainHelper.GetHomeGrain(GrainFactory, newLeader);
                var newLeaderHomeModel = await newLeaderHome.GetAllianceDetailedHomeModel();

                await ChangeMemberInfoAsync(newLeader, newLeaderHomeModel);

                await SendStreamEntryAsync(new AllianceEventStreamEntry
                {
                    AuthorId = accountId,
                    AuthorName = name,
                    AuthorRole = (int)member.Role,
                    EventType = 5,
                    TargetEntry =
                        new EventStreamTargetEntry
                            { AccountId = newLeader, Name = newLeaderHomeModel.DisplayData.AvatarName }
                });
            }
            catch
            {
                // ignored
            }
        }

        await SendMemberRemovedMessageAsync(accountId);

        await SendStreamEntryAsync(new AllianceEventStreamEntry
        {
            AuthorId = accountId,
            AuthorName = name,
            AuthorRole = 0,
            EventType = 4
        });

        await SaveAllianceAsync();
        return 0;
    }

    public async Task<int> KickMemberAsync(long kickerId, string kickerName, long kickedId)
    {
        if (!State.Members.TryGetValue(kickerId, out var kicker))
            return -1;

        if (!State.Members.TryGetValue(kickedId, out var kicked))
            return -2;

        if (kickerId == kickedId)
            return -3;

        if (!CanKickMember(kicker.Role, kicked.Role))
            return -4;

        if (kicker.Role == AllianceRole.Elder)
            if (_elderKicks.TryGetValue(kickerId, out var endTime))
                if (DateTime.UtcNow < endTime)
                    return -5;

        string kickedName;

        try
        {
            var kickedHome = GrainHelper.GetHomeGrain(GrainFactory, kickedId);
            kickedName = (await kickedHome.GetAllianceIdAndAvatarName()).Item2;
        }
        catch
        {
            return -6;
        }

        State.Members.Remove(kickedId);
        State.BannedMembers[kickedId] = DateTime.UtcNow + TimeSpan.FromHours(24);

        _elderKicks[kickerId] = DateTime.UtcNow + TimeSpan.FromHours(1);

        await SendMemberRemovedMessageAsync(kickedId);

        await SendEventStreamEntryAsync(kickerId, kickerName, 1,
            new EventStreamTargetEntry { AccountId = kickedId, Name = kickedName });

        await SaveAllianceAsync();
        return 0;
    }

    public async Task<int> ChangeMemberRoleAsync(long changerId, long changeableId, AllianceRole newRole,
        DetailedAllianceMemberHomeModel changerModel)
    {
        if (!State.Members.TryGetValue(changerId, out var changer))
            return -1;

        if (!State.Members.TryGetValue(changeableId, out var changeable))
            return -2;

        if (changerId == changeableId)
            return -3;

        var oldRole = changeable.Role;

        if (oldRole == newRole)
            return -4;

        if (!CanChangeRole(changer.Role, changeable.Role, newRole))
            return -5;

        DetailedAllianceMemberHomeModel? changeableModel;

        try
        {
            var changeableHome = GrainHelper.GetHomeGrain(GrainFactory, changeableId);
            changeableModel = await changeableHome.GetAllianceDetailedHomeModel();
        }
        catch
        {
            return -6;
        }

        if (newRole == AllianceRole.Leader)
        {
            if (changeable.Role != AllianceRole.CoLeader)
                return -7;

            changeable.Role = AllianceRole.Leader;
            changer.Role = AllianceRole.CoLeader;
        }
        else
        {
            changeable.Role = newRole;
        }

        var t = GetRoleChangeType(oldRole, newRole);

        try
        {
            await ChangeMemberInfoAsync(changeableId, changeableModel);
            await SendEventStreamEntryAsync(changerId, changerModel.DisplayData.AvatarName, t,
                new EventStreamTargetEntry
                    { AccountId = changeableId, Name = changeableModel.DisplayData.AvatarName });

            if (newRole == AllianceRole.Leader)
            {
                await ChangeMemberInfoAsync(changerId, changerModel);
                await SendEventStreamEntryAsync(changerId, changerModel.DisplayData.AvatarName, 6,
                    new EventStreamTargetEntry
                        { AccountId = changerId, Name = changerModel.DisplayData.AvatarName });
            }
        }
        catch
        {
            // ignored
        }

        await SaveAllianceAsync(updateOpenSearch: false);
        return t + 76;
    }

    public async Task<int> BanMemberAsync(long accountId, TimeSpan banTime)
    {
        if (State.Members.Count == 0)
            return -101;

        if (State.Members.ContainsKey(accountId))
            return -1;

        State.BannedMembers[accountId] = DateTime.UtcNow + banTime;

        await SaveAllianceAsync(updateOpenSearch: false);
        return 0;
    }

    public async Task ChangeMemberStatusAsync(long accountId, int status, bool inTeam)
    {
        if (!State.Members.ContainsKey(accountId)) return;

        var messageStruct = LaserContractSerializer.SerializeToStruct(new AllianceOnlineStatusUpdatedMessage
        {
            StatusChangeEntries = [new StatusChangeEntry { AccountId = accountId, Status = status }]
        });

        await SendMessagesToMembersAsync([messageStruct]);
    }

    public async Task ChangeMemberInfoAsync(long accountId, DetailedAllianceMemberHomeModel model)
    {
        if (!State.Members.TryGetValue(accountId, out var value)) return;

        var member = new AllianceMemberEntry
        {
            AccountId = accountId,
            Role = (int)value.Role,
            Trophies = model.Trophies,
            Status = model.Status,
            LastOnlineTime = model.LastOnlineTime,
            DisplayData = model.DisplayData,
            InvitesBlocked = model.InvitesBlocked
        };

        var message = new AllianceMemberMessage
        {
            AllianceId = State.AllianceId,
            MemberEntry = member
        };

        var messageStruct = LaserContractSerializer.SerializeToStruct(message);

        await SendMessagesToMembersAsync([messageStruct]);
    }

    public Task<Dictionary<long, AllianceMember>> GetMembersAsync()
    {
        return Task.FromResult(State.Members);
    }

    public ValueTask<bool> PlayerInTheClub(long accountId)
    {
        return ValueTask.FromResult(State.Members.ContainsKey(accountId));
    }

    public Task<Dictionary<long, DateTime>> GetBannedMembersAsync()
    {
        return Task.FromResult(State.BannedMembers);
    }

    public async Task<int> SendJoinRequestAsync(long accountId, int nowTrophies, PlayerDisplayData displayData,
        string text)
    {
        if (await PlayerInTheClub(accountId))
            return -1;

        if (State.AllianceType is not 2 || State.Members.Count == 0)
            return -2;

        if (nowTrophies < State.RequiredTrophies)
            return -3;

        if (_streamEntries.Any(x =>
                x.AuthorId == accountId &&
                x.GetStreamEntryType() == 3 &&
                ((JoinRequestAllianceStreamEntry)x).State == 1))
            return -4;

        if (State.BannedMembers.TryGetValue(accountId, out var endTimeM))
            if (DateTime.UtcNow < endTimeM)
                return -5;

        await SendStreamEntryAsync(new JoinRequestAllianceStreamEntry
        {
            AuthorId = accountId,
            AuthorName = displayData.AvatarName,
            AuthorRole = 0,
            Text = text,
            State = 1,
            DisplayData = displayData
        });

        return 0;
    }

    public async Task<int> JoinRequestActionAsync(long streamId, bool accepted, long actionerAccountId,
        string actionerName)
    {
        if (!State.Members.TryGetValue(actionerAccountId, out var actioner))
            return -1;

        if (actioner.Role is not (AllianceRole.Leader or AllianceRole.CoLeader or AllianceRole.Elder))
            return -2;

        var stream = _streamEntries.FirstOrDefault(x => x.StreamEntryId == streamId);

        if (stream == null)
            return -3;

        if (stream is not JoinRequestAllianceStreamEntry jstream)
            return -4;

        if (jstream.State != 1)
            return -5;

        if (accepted)
            try
            {
                var home = GrainHelper.GetHomeGrain(GrainFactory, stream.AuthorId);

                var jres = await JoinMemberAsync(
                    new AllianceMember { AccountId = stream.AuthorId, Role = AllianceRole.Member },
                    await home.GetAllianceDetailedHomeModel(), actionerAccountId, actionerName);

                if (jres != 0)
                    return -1000 + jres;

                var res = await home.SetAllianceIdAndGetAllianceDetailedHomeModel(State.AllianceId);

                if (!res.Item1 || res.Item2 == null)
                    return -6;
            }
            catch
            {
                return -7;
            }
        else
            State.BannedMembers[stream.AuthorId] = DateTime.UtcNow + TimeSpan.FromHours(24);

        jstream.ResponderName = actionerName;
        jstream.Text = string.Empty;

        jstream.AuthorRole = accepted ? 1 : 0;
        jstream.State = accepted ? 2 : 3;

        await SendStreamEntryAsync(jstream, false);

        return accepted ? 1 : 2;
    }

    public ValueTask<byte[]?> GetStreamEntryById(long id)
    {
        var res = _streamEntries.FirstOrDefault(x => x.StreamEntryId == id);

        if (res == null)
            return ValueTask.FromResult<byte[]?>(null);

        try
        {
            var bytes = MessagePackSerializer.Serialize(res);
            return ValueTask.FromResult<byte[]?>(bytes);
        }
        catch
        {
            return ValueTask.FromResult<byte[]?>(null);
        }
    }

    public async Task SendTextStreamEntryAsync(long authorId, string authorName, string text)
    {
        if (!State.Members.TryGetValue(authorId, out var author))
            return;

        var chatEntry = new ChatStreamEntry
        {
            AuthorId = authorId,
            AuthorName = authorName,
            AuthorRole = (int)author.Role,
            Text = text
        };

        await SendStreamEntryAsync(chatEntry);
    }

    public ValueTask<List<byte[]>> GetStreamEntries()
    {
        var list = _streamEntries.ToArray().Select(x => MessagePackSerializer.Serialize(x)).ToList();

        return ValueTask.FromResult(list);
    }

    public Task<AllianceTeamEntry[]> GetActualTeamsAsync()
    {
        return Task.FromResult(_teamEntries.Select(x => x.Value).ToArray());
    }

    public async Task AddOrUpdateTeamAsync(AllianceTeamEntry teamEntry)
    {
        _teamEntries[teamEntry.TeamId] = teamEntry;

        var messageStruct = LaserContractSerializer.SerializeToStruct(new AllianceTeamsMessage
        {
            ClearTeams = false,
            AllianceTeamEntries = [teamEntry]
        });

        await SendMessagesToMembersAsync([messageStruct]);
    }

    public async Task RemoveTeamAsync(long teamId)
    {
        _teamEntries.Remove(teamId);

        var messageStruct = LaserContractSerializer.SerializeToStruct(new AllianceTeamRemovedMessage
        {
            TeamId = teamId
        });

        await SendMessagesToMembersAsync([messageStruct]);
    }

    public Task<AllianceRankingData?> GetAllianceRankingDataAsync()
    {
        if (State.CreatedDateTime == null)
            return Task.FromResult<AllianceRankingData?>(null);

        return Task.FromResult(new AllianceRankingData
        {
            AllianceId = State.AllianceId,
            AllianceName = State.Name,
            BadgeGlobalId = State.BadgeGlobalId,
            MembersCount = State.Members.Count,
            Trophies = State.CalculatedSumTrophies
        })!;
    }

    private async Task<DetailedAllianceMemberModel[]> FetchDetailedMembersAsync(AllianceMember[] members)
    {
        var memberTasks = members.Select(async member =>
        {
            try
            {
                var memberHome = GrainHelper.GetHomeGrain(GrainFactory, member.AccountId);
                var homeModel = await memberHome.GetAllianceDetailedHomeModel();

                return new DetailedAllianceMemberModel
                {
                    AllianceMember = member,
                    HomeModel = homeModel
                };
            }
            catch
            {
                return null;
            }
        });

        var memberResults = await Task.WhenAll(memberTasks);

        return memberResults
            .Where(x => x != null)
            .OrderBy(x => x!.HomeModel.Trophies)
            .ToArray()!;
    }

    private static bool CanKickMember(AllianceRole kickerRole, AllianceRole kickedRole)
    {
        return kickerRole switch
        {
            AllianceRole.Leader => true,
            AllianceRole.CoLeader => kickedRole is AllianceRole.Member or AllianceRole.Elder,
            AllianceRole.Elder => kickedRole is AllianceRole.Member,
            _ => false
        };
    }

    private static int GetRoleChangeType(AllianceRole oldRole, AllianceRole newRole)
    {
        return (oldRole, newRole) switch
        {
            (AllianceRole.Member, AllianceRole.Elder) => 5,
            (AllianceRole.Member, AllianceRole.CoLeader) => 5,
            (AllianceRole.Member, AllianceRole.Leader) => 5,
            (AllianceRole.Elder, AllianceRole.CoLeader) => 5,
            (AllianceRole.Elder, AllianceRole.Leader) => 5,
            (AllianceRole.CoLeader, AllianceRole.Leader) => 5,

            (AllianceRole.Leader, AllianceRole.CoLeader) => 6,
            (AllianceRole.Leader, AllianceRole.Elder) => 6,
            (AllianceRole.Leader, AllianceRole.Member) => 6,
            (AllianceRole.CoLeader, AllianceRole.Elder) => 6,
            (AllianceRole.CoLeader, AllianceRole.Member) => 6,
            (AllianceRole.Elder, AllianceRole.Member) => 6,

            _ => 0
        };
    }

    private async Task SendMemberRemovedMessageAsync(long accountId)
    {
        var message = new AllianceMemberRemovedMessage
        {
            AllianceId = State.AllianceId,
            AccountId = accountId
        };

        var messageStruct = LaserContractSerializer.SerializeToStruct(message);

        await SendMessagesToMembersAsync([messageStruct]);
    }

    private async Task SendMessagesToMembersAsync(PiranhaMessageStruct[] messages)
    {
        var members = State.Members.Values.ToArray();

        if (members.Length == 0)
            return;

        foreach (var member in members)
        {
            var memberSession = GrainHelper.GetPlayerSession(GrainFactory, member.AccountId);
            await memberSession.SendMessagesToPlayerAsync(messages).AsTask();
        }
    }

    private static bool CanChangeRole(AllianceRole changerRole, AllianceRole oldRole, AllianceRole newRole)
    {
        return changerRole switch
        {
            AllianceRole.Leader => true,
            AllianceRole.CoLeader => CanCoLeaderChangeRole(oldRole, newRole),
            _ => false
        };
    }

    private static bool CanCoLeaderChangeRole(AllianceRole oldRole, AllianceRole newRole)
    {
        return oldRole is not (AllianceRole.CoLeader or AllianceRole.Leader) && newRole is not AllianceRole.Leader;
    }

    private async Task RemoveStreamEntryAsync(long id)
    {
        var count = _streamEntries.RemoveAll(x => x.StreamEntryId == id);

        if (count > 0)
            await SendMessagesToMembersAsync([
                LaserContractSerializer.SerializeToStruct(
                    new AllianceStreamEntryRemovedMessage { StreamId = id })
            ]);
    }

    private long GetMostLikelyNextPresidentId()
    {
        var candidates = State.Members.ToArray()
            .Where(m => m.Value.Role != AllianceRole.Leader)
            .Select(x => x.Value)
            .ToList();

        if (candidates.Count == 0)
            return -1;

        var coLeaders = candidates.Where(m => m.Role == AllianceRole.CoLeader).ToList();

        if (coLeaders.Count > 0)
            return coLeaders.OrderBy(m => m.JoinTime).First().AccountId;

        var elders = candidates.Where(m => m.Role == AllianceRole.Elder).ToList();

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (elders.Count > 0)
            return elders.OrderBy(m => m.JoinTime).First().AccountId;

        return candidates.OrderBy(m => m.JoinTime).First().AccountId;
    }

    private async Task TickAsync()
    {
        try
        {
            await CheckMembersAsync();
            await CheckTeamsAsync();
            await CalculateSumTrophiesAsync();
            await SaveAllianceAsync();

            var region = LogicDataTables.GetDataById<LogicRegionData>(State.RegionGlobalId);

            await LeaderboardContainer.LeaderboardService.UpdateAllianceTrophiesAsync(State.AllianceId,
                State.CalculatedSumTrophies, region?.Name);
        }
        catch (Exception e)
        {
            Logger.Error(e, "Tick error!");
        }
    }

    private async Task CheckMembersAsync()
    {
        try
        {
            if (State.Members.Count == 0)
                return;

            var detailed = await GetDetailedAllianceInfoAsync();

            var tasks = State.Members.ToArray().Select(member =>
                GrainHelper.GetHomeGrain(GrainFactory, member.Value.AccountId)
                    .GetAllianceIdAndAccountIdAndLastAllianceIdChangeTime()
            );

            var results = await Task.WhenAll(tasks);

            foreach (var player in results)
                try
                {
                    if (!State.Members.TryGetValue(player.Item2, out var member)) continue;

                    if (player.Item1 <= 0)
                    {
                        await GrainHelper.GetHomeGrain(GrainFactory, player.Item2).CorrectAllianceId(State.AllianceId,
                            detailed.AllianceParams,
                            member.Role,
                            detailed.AllianceMembers!.Length, State.CalculatedSumTrophies,
                            detailed.AllianceMembers.Count(x => x.HomeModel.Status != 0));

                        continue;
                    }

                    if (player.Item1 == State.AllianceId)
                        continue;

                    if (member.JoinTime < player.Item3)
                    {
                        State.Members.Remove(member.AccountId);

                        State.CalculatedSumTrophies -= detailed.AllianceMembers?
                            .FirstOrDefault(x =>
                                x.AllianceMember.AccountId == member.AccountId)
                            ?.HomeModel.Trophies ?? 0;

                        await SendMemberRemovedMessageAsync(member.AccountId);

                        continue;
                    }

                    await GrainHelper.GetHomeGrain(GrainFactory, player.Item2).CorrectAllianceId(State.AllianceId,
                        detailed.AllianceParams,
                        member.Role,
                        detailed.AllianceMembers!.Length, State.CalculatedSumTrophies,
                        detailed.AllianceMembers.Count(x => x.HomeModel.Status != 0));
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to check member");
                }
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to check members");
        }
    }

    private async Task CheckTeamsAsync()
    {
        foreach (var teamEntry in _teamEntries.ToArray())
            try
            {
                var t = GrainHelper.GetTeamGrain(GrainFactory, teamEntry.Value.TeamId);

                var m = await t.SyncWithAllianceAsync(State.AllianceId, State.Members.Select(x => x.Key).ToArray());

                if (m.Length == 0 || m.All(x => !State.Members.ContainsKey(x)))
                {
                    await RemoveTeamAsync(teamEntry.Value.TeamId);
                    continue;
                }

                teamEntry.Value.Players = m;

                await AddOrUpdateTeamAsync(teamEntry.Value);
            }
            catch
            {
                await RemoveTeamAsync(teamEntry.Value.TeamId);
            }
    }

    private async Task CalculateSumTrophiesAsync()
    {
        try
        {
            if (State.Members.Count == 0)
            {
                State.CalculatedSumTrophies = 0;
                return;
            }

            var tasks = State.Members.ToArray().Select(member =>
                GrainHelper.GetHomeGrain(GrainFactory, member.Value.AccountId).GetNowTrophies()
            );

            var results = await Task.WhenAll(tasks);

            State.CalculatedSumTrophies = results.Sum();
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to calculate sum trophies");
        }
    }

    private async Task SaveAllianceAsync(bool create = false, bool updateOpenSearch = true)
    {
        if (State.CreatedDateTime == null)
            return;

        foreach (var bannedMember in State.BannedMembers.ToArray())
            if (DateTime.UtcNow > bannedMember.Value)
                State.BannedMembers.Remove(bannedMember.Key);

        if (State.Members.Count == 0)
        {
            State.CalculatedSumTrophies = 0;

            _tickTimer?.Dispose();
            _tickTimer = null;
        }

        await WriteStateAsync();

        if (!updateOpenSearch) return;

        var doc = new OpenSearchAlliance
        {
            Id = State.AllianceId,
            Name = State.Name,
            AllianceType = State.AllianceType,
            RegionGlobalId = State.RegionGlobalId,
            LanguageGlobalId = State.LanguageGlobalId,
            BadgeGlobalId = State.BadgeGlobalId,
            NowTrophies = State.CalculatedSumTrophies,
            RequiredTrophies = State.RequiredTrophies,
            MembersCount = State.Members.Count
        };

        if (create)
            await openSearchWorker.CreateAllianceAsync(doc);
        else
            await openSearchWorker.UpsertAllianceAsync(doc); // or update :)
    }

    private async Task SendEventStreamEntryAsync(long authorId, string authorName, int eventType,
        EventStreamTargetEntry? targetEntry = null)
    {
        if (!State.Members.TryGetValue(authorId, out var author))
            return;

        var eventEntry = new AllianceEventStreamEntry
        {
            AuthorId = authorId,
            AuthorName = authorName,
            AuthorRole = (int)author.Role,
            EventType = eventType,
            TargetEntry = targetEntry
        };

        await SendStreamEntryAsync(eventEntry);
    }

    private async Task SendStreamEntryAsync(StreamEntry streamEntry, bool changeStreamEntryId = true)
    {
        if (changeStreamEntryId)
            streamEntry.StreamEntryId = Random.Shared.NextInt64();

        streamEntry.SendTime = DateTime.UtcNow;

        _streamEntries.Add(streamEntry);

        var message = new AllianceStreamEntryMessage
        {
            StreamEntry = streamEntry
        };

        var messageStruct = LaserContractSerializer.SerializeToStruct(message);

        if (_streamEntries.Count > Settings.AllianceSettings.GetConfig().MaxMessagesInChat)
        {
            var fistStreamEntry = _streamEntries.First();

            _streamEntries.Remove(fistStreamEntry);

            var message2 = new AllianceStreamEntryRemovedMessage
            {
                StreamId = fistStreamEntry.StreamEntryId
            };

            var messageStruct2 = LaserContractSerializer.SerializeToStruct(message2);

            await SendMessagesToMembersAsync([messageStruct, messageStruct2]);
            return;
        }

        await SendMessagesToMembersAsync([messageStruct]);
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var allianceId = this.GetPrimaryKeyString().Split('_').Last();

        State.AllianceId = Convert.ToInt64(allianceId);

        if (State is { CreatedDateTime: not null, Members.Count: > 0 })
            _tickTimer ??= this.RegisterGrainTimer<object?>(
                async _ => await TickAsync(),
                null,
                new GrainTimerCreationOptions
                {
                    DueTime = TimeSpan.FromSeconds(50),
                    Period = TimeSpan.FromMinutes(5),
                    Interleave = false
                });

        return base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        if (State.CreatedDateTime == null || State.Members.Count == 0) return;

        _tickTimer?.Dispose();
        _tickTimer = null;

        await SaveAllianceAsync();

        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}