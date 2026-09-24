using MessagePack;
using NLog;
using Orleans.Providers;
using ZLinq;
using ZOVserver.Services.Game.HomeService.Laser.Commands;
using ZOVserver.Services.Game.HomeService.Laser.Messages;
using ZOVserver.Services.Game.HomeService.Laser.Mode;
using ZOVserver.Services.Game.HomeService.Leaderboard;
using ZOVserver.Services.Game.HomeService.Settings;
using ZOVserver.Services.Game.HomeService.States;
using ZOVserver.Shared.Abstractions;
using ZOVserver.Shared.Contracts.Events;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Interfaces;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;
using ZOVserver.Shared.Contracts.Laser.Combined.Entries;
using ZOVserver.Shared.Contracts.Laser.Combined.Friend;
using ZOVserver.Shared.Contracts.Laser.Combined.Home;
using ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;
using ZOVserver.Shared.Contracts.Laser.Combined.Notifications;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;
using ZOVserver.Shared.Contracts.Laser.Combined.Team;
using ZOVserver.Shared.Contracts.Laser.Commands.ToClient;
using ZOVserver.Shared.Contracts.Laser.Machine;
using ZOVserver.Shared.Contracts.Laser.Messages;
using ZOVserver.Shared.Contracts.Laser.Messages.Client;
using ZOVserver.Shared.Contracts.Laser.Messages.Server;
using ZOVserver.Shared.Contracts.Models;
using ZOVserver.Shared.Contracts.Structs;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Services.Game.HomeService.Grains;

[StorageProvider(ProviderName = "MongoStorage")]
// ReSharper disable once UnusedType.Global
public class HomeGrain(ITeamPlayersSearchService teamPlayersSearchService) : Grain<HomeState>, IHomeServiceGrain
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private int _battleProblemCounter;
    private IBridgeObserver? _bridgeObserver;

    private CommandManager? _commandManager;
    private LogicHomeMode? _logicHomeMode;

    private MessageManager? _messageManager;

    private IDisposable? _tickTimer;

    public async ValueTask OnConnectedAsync(string serverIp, int serverPort, string clientIp, int clientPort,
        Guid sessionId)
    {
        State.GameState = 1;
        State.SessionId = sessionId;

        try
        {
            if (State.BattleId != null)
            {
                var battleServiceGrain = GrainHelper.GetBattleGrain(GrainFactory, State.BattleId.Value);

                var r = await battleServiceGrain.IsBattleActiveWithPlayersAsync([State.AccountId]);

                if (!r.Item1 || !r.Item2)
                    State.BattleId = null;
            }
        }
        catch
        {
            State.BattleId = null;
        }

        _tickTimer ??= this.RegisterGrainTimer<object?>(
            async _ => await TickAsync(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            null,
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromSeconds(1),
                Period = TimeSpan.FromSeconds(3),
                Interleave = false
            });

        var playerSession = GrainHelper.GetPlayerSession(GrainFactory, State.AccountId);

        _messageManager ??=
            new MessageManager(this, State, playerSession, GrainFactory, teamPlayersSearchService);

        _commandManager ??= new CommandManager(this, State, _messageManager, GrainFactory);
        _messageManager.CommandManager ??= _commandManager;

        _logicHomeMode ??=
            new LogicHomeMode(this, State, _messageManager, _commandManager, playerSession, GrainFactory);

        _messageManager.HomeMode ??= _logicHomeMode;
        _commandManager.HomeMode ??= _logicHomeMode;

        _commandManager.InitExecutors();

        State.LastKeepAliveReceivedTime = DateTime.UtcNow;

        if (State.HomeCreatedTime != default)
            await WriteStateAsync();
    }

    public async ValueTask OnDisconnectedAsync(DateTime disconnectTime, bool isAccountSessionSwitched)
    {
        if (State.GameState == 0 && State.SessionId == Guid.Empty)
            return;

        State.GameState = 0;
        State.SessionId = Guid.Empty;

        _tickTimer?.Dispose();
        _tickTimer = null;

        _battleProblemCounter = 0;

        if (_messageManager != null)
            await _messageManager.GoodbyeAsync();
        _messageManager = null;

        if (_commandManager != null)
            await _commandManager.GoodbyeAsync();
        _commandManager = null;

        if (_logicHomeMode != null)
            await _logicHomeMode.GoodbyeAsync();
        _logicHomeMode = null;

        if (State.HomeCreatedTime != default)
            await WriteStateAsync();

        _ = LeaderboardContainer.LeaderboardService.UpdatePlayerFullDataAsync(State.AccountId,
            null, State.Region,
            State.HeroEntries.ToArray().ToDictionary(x => x.CharacterGlobalId, y => y.Trophies));
    }

    public ValueTask<int> GetGrainGameState()
    {
        return ValueTask.FromResult(State.GameState);
    }

    public ValueTask<int> SetGrainGameState(int gameState)
    {
        return ValueTask.FromResult(State.GameState = gameState);
    }

    public ValueTask<long> GetAccountId()
    {
        return ValueTask.FromResult(State.AccountId);
    }

    public ValueTask<IReadOnlyList<(int messageType, string messageName)>> GetAllImplementedMessagesAsync()
    {
        return _messageManager == null
            ? ValueTask.FromResult<IReadOnlyList<(int messageType, string messageName)>>([])
            : ValueTask.FromResult(MessageManager.GetAllImplementedMessages());
    }

    public async ValueTask<bool> ReceiveImplementedMessagesAsync(PiranhaMessageStruct[] piranhaMessages)
    {
        if (_messageManager == null)
            return false;

        foreach (var message in piranhaMessages)
        {
            var piranhaMessage = LogicLaserMessageFactory.CreateMessageByType(message.MessageType);
            if (piranhaMessage == null) continue;

            LaserContractSerializer.Deserialize(piranhaMessage, message.MessagePayload);

            var res = await _messageManager.ReceiveMessageAsync(piranhaMessage);

            Logger.Debug($"New message: {piranhaMessage.GetMessageTypeName()} received. Result: {res}.");

            if (res < 0)
                return false;
        }

        return true;
    }

    public async ValueTask<bool> SendMessagesToPlayerAsync(PiranhaMessageStruct[] messages)
    {
        if (State.GameState == 0 || State.SessionId == Guid.Empty)
            return false;

        await SendBridgeEventToClientAsync(
            new BridgeEvent { EventType = 20, SessionId = State.SessionId, PiranhaMessages = messages });

        Logger.Debug($"Sending {messages.Length} messages to {State.SessionId}.");
        return true;
    }

    public ValueTask<DetailedAllianceMemberHomeModel> GetAllianceDetailedHomeModel()
    {
        return ValueTask.FromResult(new DetailedAllianceMemberHomeModel
        {
            DisplayData = new PlayerDisplayData
            {
                AvatarName = State.AvatarName,
                Experience = State.Experience,
                NameColor = State.NameColorGlobalId,
                Thumbnail = State.ThumbnailGlobalId
            },
            Status = State.PlayerStatus,
            LastOnlineTime = State.LastKeepAliveReceivedTime,
            Trophies = State.NowTrophies,
            InvitesBlocked = State.BlockInvites
        });
    }

    public Task<(long, long, DateTime)> GetAllianceIdAndAccountIdAndLastAllianceIdChangeTime()
    {
        return Task.FromResult((State.AllianceId, State.AccountId, State.LastAllianceIdChangeTime));
    }

    public Task<(HeroEntry[], ProfileStatEntry[], PlayerDisplayData)> GetProfileData()
    {
        var entrs = State.HeroEntries.ToArray().Select(x => new HeroEntry
        {
            CharacterGlobalId = x.CharacterGlobalId,
            UnkDataRef = x.UnkDataRef,
            CardUnlockGlobalId = x.CardUnlockGlobalId,
            CharacterState = x.CharacterState,
            Trophies = x.Trophies,
            MaxTrophies = x.MaxTrophies,
            PowerLevel = x.PowerLevel - 1,
            PowerPoints = x.PowerPoints,
            StarPowersContainer = new Dictionary<int, bool>(x.StarPowersContainer)
        }).ToArray();

        ProfileStatEntry[] pses =
        [
            new() { Id = 1, Value = State.TrioWins },
            new() { Id = 2, Value = State.Experience },
            new() { Id = 3, Value = State.NowTrophies },
            new() { Id = 4, Value = State.MaxTrophies },
            new() { Id = 5, Value = entrs.Length },
            new() { Id = 6, Value = State.NameColorGlobalId },
            new() { Id = 7, Value = State.ThumbnailGlobalId },
            new() { Id = 8, Value = State.SoloWins },
            new() { Id = 9, Value = State.BestRoboRumbleTime },
            new() { Id = 10, Value = State.BestBigGameBossTime },
            new() { Id = 11, Value = State.DuoWins },
            new() { Id = 12, Value = State.BestRaidBossLevel },
            new() { Id = 14, Value = State.BestRankInLeague }
        ];

        var dd = new PlayerDisplayData
        {
            AvatarName = State.AvatarName,
            Experience = State.Experience,
            NameColor = State.NameColorGlobalId,
            Thumbnail = State.ThumbnailGlobalId
        };

        return Task.FromResult((entrs, pses, dd));
    }

    public async Task UpdateMyFriendEntryInFriendshipAsync()
    {
        var friendship = GrainHelper.GetFriendshipGrain(GrainFactory, State.AccountId);
        await friendship.ChangeFriendEntryAsync(await GetFriendEntryAsync());
    }

    public async Task UpdateMyFriendOnlineStatusEntryInFriendshipAsync()
    {
        var friendship = GrainHelper.GetFriendshipGrain(GrainFactory, State.AccountId);
        await friendship.ChangeMyFriendOnlineStatusEntryAsync(State.AccountId, await GetFriendOnlineStatusEntryAsync());
    }

    public async Task UpdateAllianceTeamEntryInFriendshipAsync(AllianceTeamEntry? allianceTeamEntry)
    {
        var friendship = GrainHelper.GetFriendshipGrain(GrainFactory, State.AccountId);

        if ((DateTime.UtcNow - State.LastKeepAliveReceivedTime).TotalSeconds > 35 || State.GameState == 0)
        {
            await friendship.ChangeMyFriendOnlineStatusEntryAsync(State.AccountId, null);
            return;
        }

        var entry = new FriendOnlineStatusEntry
        {
            AccountId = State.AccountId,

            Status = State.PlayerStatus,

            InvitesBlocked = State.BlockInvites,

            AllianceTeamEntry = allianceTeamEntry
        };

        await friendship.ChangeMyFriendOnlineStatusEntryAsync(State.AccountId, entry);
    }

    public ValueTask<FriendEntry> GetFriendEntryAsync()
    {
        return ValueTask.FromResult(
            new FriendEntry
            {
                AccountId = State.AccountId,

                Trophies = State.NowTrophies,

                Alliance = State.AllianceId <= 0
                    ? null
                    : new FriendAllianceSegment
                    {
                        AllianceId = State.AllianceId
                    },

                LastOnlineTime = (DateTime.UtcNow - State.LastKeepAliveReceivedTime).TotalSeconds > 35
                    ? State.LastKeepAliveReceivedTime
                    : default,

                DisplayData = new PlayerDisplayData
                {
                    AvatarName = State.AvatarName,
                    Experience = State.Experience,
                    NameColor = State.NameColorGlobalId,
                    Thumbnail = State.ThumbnailGlobalId
                }
            });
    }

    public async ValueTask<FriendOnlineStatusEntry?> GetFriendOnlineStatusEntryAsync()
    {
        if ((DateTime.UtcNow - State.LastKeepAliveReceivedTime).TotalSeconds > 35 || State.GameState == 0)
            return null;

        return new FriendOnlineStatusEntry
        {
            AccountId = State.AccountId,

            Status = State.PlayerStatus,

            InvitesBlocked = State.BlockInvites,

            AllianceTeamEntry = await GetAllianceTeamEntryAsync()
        };

        async ValueTask<AllianceTeamEntry?> GetAllianceTeamEntryAsync()
        {
            try
            {
                var team = GrainHelper.GetTeamGrain(GrainFactory, State.TeamId);
                return await team.GetAllianceTeamEntry();
            }
            catch
            {
                return null;
            }
        }
    }

    public async ValueTask<(FriendEntry, FriendOnlineStatusEntry?)> SyncFriendshipWithHomeAsync()
    {
        return (await GetFriendEntryAsync(), await GetFriendOnlineStatusEntryAsync());
    }

    public async Task CorrectAllianceId(long newId, AllianceParams? allianceParams = null, AllianceRole? role = null,
        int? membersCount = null, int? nowTrophies = null, int? onlineMembers = null)
    {
        if (State.AllianceId > 0 && newId != State.AllianceId)
        {
            var alliance = GrainHelper.GetAllianceGrain(GrainFactory, State.AllianceId);
            _ = alliance.LeaveMemberAsync(State.AccountId, State.AvatarName);

            if (_messageManager != null)
                await _messageManager.SendMessagesAsync(
                    new MyAllianceMessage(),
                    new AllianceTeamsMessage { ClearTeams = true });
        }

        State.AllianceId = newId;
        State.LastAllianceIdChangeTime = DateTime.UtcNow;

        await UpdateMyFriendEntryInFriendshipAsync();

        if (State.TeamId > 0)
            try
            {
                var team = GrainHelper.GetTeamGrain(GrainFactory, State.TeamId);
                _ = await team.AddOrChangeMemberAllianceIdAsync(State.AccountId, State.AllianceId);
            }
            catch
            {
                // ignored.
            }

        if (newId <= 0 || _messageManager == null || allianceParams == null) return;

        var header = new AllianceHeaderEntry
        {
            AllianceId = allianceParams.AllianceId,
            AllianceName = allianceParams.Name,
            BadgeGlobalId = allianceParams.BadgeGlobalId,
            AllianceType = allianceParams.AllianceType,
            MembersCount = membersCount ?? 0,
            NowTrophies = nowTrophies ?? 0,
            RequiredTrophies = allianceParams.RequiredTrophies,
            PreferredLanguageGlobalId = allianceParams.LanguageGlobalId,
            Region = LogicDataTables.GetDataById(allianceParams.RegionGlobalId)?.Name ?? "RU"
        };

        await _messageManager.SendMessagesAsync(
            new MyAllianceMessage
            {
                OnlineMembers = onlineMembers ?? 0,
                MyAllianceObject = new MyAllianceObject
                {
                    MyRoleDataRef = ((int?)role ?? 0) + 25_000_000,
                    AllianceHeaderEntry = header
                }
            });
    }

    public async Task<(bool, DetailedAllianceMemberHomeModel?)> SetAllianceIdAndGetAllianceDetailedHomeModel(long id)
    {
        if (State.AllianceId > 0)
            return (false, null);

        await CorrectAllianceId(id);

        return (true, await GetAllianceDetailedHomeModel());
    }

    public async Task RemoveTeamRequest(long teamId, int status)
    {
        _messageManager?.TeamRequests.Remove(teamId);

        switch (status)
        {
            case 0 when _messageManager != null:
                await _messageManager.SendMessagesAsync(new TeamErrorMessage { ErrorCode = 41 });
                break;
            case 1:
                State.TeamId = teamId;
                State.TeamEventSlot = 0;

                if (State.AllianceId > 0)
                {
                    var team = GrainHelper.GetTeamGrain(GrainFactory, teamId);
                    _ = team.AddOrChangeMemberAllianceIdAsync(State.AccountId, State.AllianceId);
                }

                break;
            case 2 when _messageManager != null:
                await _messageManager.SendMessagesAsync(new TeamErrorMessage { ErrorCode = 45 });
                break;
            case 3 when _messageManager != null:
                await _messageManager.SendMessagesAsync(new TeamErrorMessage { ErrorCode = 43 });
                break;
            case 4 when _messageManager != null:
                await _messageManager.SendMessagesAsync(new TeamErrorMessage { ErrorCode = 44 });
                break;
        }
    }

    public async Task KickFromAlliance()
    {
        await CorrectAllianceId(0);
    }

    public async Task SendMyAllianceAsync()
    {
        try
        {
            if (_messageManager == null)
                return;

            if (State.AllianceId <= 0)
            {
                await _messageManager.SendMessagesAsync(new MyAllianceMessage());
                return;
            }

            var alliance = GrainHelper.GetAllianceGrain(GrainFactory, State.AllianceId);

            if (!await alliance.PlayerInTheClub(State.AccountId))
            {
                await CorrectAllianceId(0);
                return;
            }

            var data = await alliance.GetDetailedAllianceInfoAsync();

            if (data.AllianceMembers == null) return;

            var myRole = (int?)data.AllianceMembers
                .FirstOrDefault(x => x.AllianceMember.AccountId == State.AccountId)?
                .AllianceMember.Role;

            var header = new AllianceHeaderEntry
            {
                AllianceId = data.AllianceParams.AllianceId,
                AllianceName = data.AllianceParams.Name,
                BadgeGlobalId = data.AllianceParams.BadgeGlobalId,
                AllianceType = data.AllianceParams.AllianceType,
                MembersCount = data.AllianceMembers.Length,
                NowTrophies = data.AllianceMembers.Sum(x => x.HomeModel.Trophies),
                RequiredTrophies = data.AllianceParams.RequiredTrophies,
                PreferredLanguageGlobalId = data.AllianceParams.LanguageGlobalId,
                Region = LogicDataTables.GetDataById(data.AllianceParams.RegionGlobalId)?.Name ?? "RU"
            };

            var memberEntries = data.AllianceMembers.Select(x => new AllianceMemberEntry
            {
                AccountId = x.AllianceMember.AccountId,
                Role = (int)x.AllianceMember.Role,
                Trophies = x.HomeModel.Trophies,
                Status = x.HomeModel.Status,
                LastOnlineTime = x.HomeModel.LastOnlineTime,
                InvitesBlocked = x.HomeModel.InvitesBlocked,
                DisplayData = x.HomeModel.DisplayData
            }).ToList();

            await _messageManager.SendMessagesAsync(
                new MyAllianceMessage
                {
                    OnlineMembers = data.AllianceMembers.Count(x => x.HomeModel.Status != 0),
                    MyAllianceObject = new MyAllianceObject
                    {
                        MyRoleDataRef = (myRole ?? 0) + 25_000_000,
                        AllianceHeaderEntry = header
                    }
                },
                new AllianceDataMessage
                {
                    IsMyAlliance = true,
                    AllianceFullEntry = new AllianceFullEntry
                    {
                        AllianceHeaderEntry = header,
                        Description = data.AllianceParams.Description,
                        Members = memberEntries
                    }
                },
                new AllianceTeamsMessage
                {
                    ClearTeams = true,
                    AllianceTeamEntries = data.TeamEntries.ToList()
                });

            await _messageManager.ReceiveMessageAsync(new AskForAllianceStreamMessage());
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }
    }

    public Task<long> GetTeamId()
    {
        return Task.FromResult(State.TeamId);
    }

    public async Task<(int, string)> GetIsPossibleToTeamInviteAndAvatarName(long teamId, long inviterId,
        long inviterAllianceId)
    {
        return State.BlockInvites switch
        {
            true => (-7, State.AvatarName),
            _ when State.TeamId > 0 => (-6, State.AvatarName),
            _ when (DateTime.UtcNow - State.LastKeepAliveReceivedTime).TotalSeconds > 35 =>
                (-5, State.AvatarName),
            _ when (_messageManager?.MutedPlayers.TryGetValue(inviterId, out var endTime) ?? false) &&
                   endTime > DateTime.UtcNow
                => (-4, State.AvatarName),
            _ when State.AvatarName.Length < 3 => (-3, State.AvatarName),
            _ when inviterAllianceId > 0 && State.AllianceId != inviterAllianceId =>
                (-2, State.AvatarName),
            _ when inviterAllianceId == 0 && await IsNotMyFriend(inviterId) => (-1, State.AvatarName),
            _ => (1, State.AvatarName)
        };

        async Task<bool> IsNotMyFriend(long accountId)
        {
            try
            {
                var friendship = GrainHelper.GetFriendshipGrain(GrainFactory, State.AccountId);
                return !await friendship.IsMyFriend(accountId);
            }
            catch
            {
                return true;
            }
        }
    }

    public async Task<int> GetIsPossibleToTeamRequest(long requesterId, long requesterAllianceId)
    {
        if ((_messageManager?.MutedPlayers.TryGetValue(requesterId, out var endTime) ?? false) &&
            endTime > DateTime.UtcNow)
            return -1;

        switch (requesterAllianceId)
        {
            case > 0 when State.AllianceId != requesterAllianceId:
            {
                if (await IsNotMyFriend(requesterId))
                    return -2;

                goto l2;
            }
            case 0 when await IsNotMyFriend(requesterId):
                return -3;
        }

        l2:
        if ((DateTime.UtcNow - State.LastKeepAliveReceivedTime).TotalSeconds > 35)
            return -4;

        if (State.BlockInvites)
            return -5;

        return 1;

        async Task<bool> IsNotMyFriend(long accountId)
        {
            try
            {
                var friendship = GrainHelper.GetFriendshipGrain(GrainFactory, State.AccountId);
                return !await friendship.IsMyFriend(accountId);
            }
            catch
            {
                return true;
            }
        }
    }

    public async Task<int> TeamInviteCancelAsync(long teamId)
    {
        if (_messageManager == null)
            return -1;

        var r = _messageManager.TeamInvites.TryRemove(teamId, out var friend);

        if (!r)
            return -2;

        await SendMessagesToPlayerAsync([
            LaserContractSerializer.SerializeToStruct(
                new TeamInvitationMessage
                {
                    Type = 0,
                    TeamInvitation = new TeamInvitation
                    {
                        TeamId = teamId,
                        FriendEntry = new FriendEntry
                        {
                            AccountId = friend!.AccountId,
                            DisplayData = new PlayerDisplayData
                            {
                                AvatarName = friend.DisplayData?.AvatarName ?? "inviter",
                                Experience = 0,
                                NameColor = GlobalId.CreateGlobalId(43, 0),
                                Thumbnail = GlobalId.CreateGlobalId(28, 0)
                            }
                        }
                    }
                })
        ]);

        return 0;
    }

    public async Task<int> TeamInviteAsync(long teamId, FriendEntry inviter)
    {
        if (_messageManager == null || State.TeamId > 0)
            return -1;

        var r = _messageManager.TeamInvites.TryAdd(teamId, inviter);

        if (!r)
            return -2;

        r = await _messageManager.SendMessagesAsync(new TeamInvitationMessage
        {
            Type = 1,
            TeamInvitation = new TeamInvitation { TeamId = teamId, FriendEntry = inviter }
        });

        if (r)
        {
            _messageManager.TeamIdsWithMyTrash.Add(teamId);
            return 0;
        }

        _messageManager.TeamInvites.TryRemove(teamId, out _);
        return -3;
    }

    public async Task KickFromTeamAsync(long teamId)
    {
        if (State.TeamId != teamId)
            return;

        State.TeamId = 0;

        if (_messageManager != null)
            await _messageManager.SendMessagesAsync(new TeamLeftMessage { Reason = 1 });
    }

    public Task<TeamMemberData?> GetBasicTeamMemberData()
    {
        if (_logicHomeMode == null || State.TeamId > 0)
            return Task.FromResult<TeamMemberData?>(null);

        var r = _logicHomeMode.IsHeroUnlocked(State.HomeBrawlerGlobalId, out var hero);

        if (!r)
            return Task.FromResult<TeamMemberData?>(null);

        return Task.FromResult(new TeamMemberData
        {
            DisplayData = new PlayerDisplayData
            {
                AvatarName = State.AvatarName,
                Experience = State.Experience,
                Thumbnail = State.ThumbnailGlobalId,
                NameColor = State.NameColorGlobalId
            },

            CharacterGlobalId = hero!.CharacterGlobalId,
            SkinGlobalId = State.SelectedSkins.FirstOrDefault(x =>
                LogicHomeMode.SkinIdToCharacterData[x].GlobalId == hero.CharacterGlobalId),

            HeroTrophies = hero.Trophies,
            HeroMaxTrophies = hero.MaxTrophies,
            HeroPowerLevel = hero.PowerLevel,

            StarPowerGlobalId = hero.StarPowersContainer.FirstOrDefault(x => x.Value).Key,

            DifficultyLevel = State.Events.FirstOrDefault(x => x.TicketEventDifficulty > 0)?.TicketEventDifficulty ?? 0
        })!;
    }

    public async Task SendOwnHomeDataAsync(bool first = true)
    {
        await CorrectSupportedContentCreator();

        var v1 = await CreateMyBattleLoadingAsync();

        if (v1)
            return;

        if (_logicHomeMode != null)
            await _logicHomeMode.CreateHomeEvents(false);

        if (_messageManager != null)
            await _messageManager.SendMessagesAsync(new OwnHomeDataMessage
            {
                Capacity = 50000,

                Home = new LogicClientHome
                {
                    DailyData = _logicHomeMode?.CreateDailyData(),
                    ConfData = _logicHomeMode?.CreateConfData(),

                    Notifications = State.Notifications,
                    HomeId = State.HomeId
                },

                Avatar = new LogicClientAvatar
                {
                    AccountId = State.AccountId,
                    HomeId = State.HomeId,
                    AvatarName = State.AvatarName,
                    NameSetByUser = State.NameSetByUser,
                    HeroEntries = State.HeroEntries,
                    MiniBoxTokens = State.MiniBoxTokens,
                    BigBoxStarTokens = State.BigBoxStarTokens,
                    Gold = State.Gold,
                    Diamonds = State.Diamonds,
                    StarPoints = State.StarPoints,
                    TutorialState = State.TutorialState
                }
            });
    }

    public ValueTask<DateTime> GetLastKeepAliveReceivedTime()
    {
        return ValueTask.FromResult(State.LastKeepAliveReceivedTime);
    }

    public async Task InitiateBattleEntryAsync()
    {
        _ = await CreateMyBattleLoadingAsync();
    }

    public ValueTask SetB(byte b)
    {
        _messageManager?.B = b;
        return ValueTask.CompletedTask;
    }

    public async ValueTask SaveHomeStateAsync()
    {
        await WriteStateAsync();
    }

    public async Task TickAsync(long timestamp)
    {
        if (State.GameState == 0) return;

        try
        {
            if (_messageManager != null)
                await _messageManager.TickAsync();

            if (_commandManager != null)
                await _commandManager.TickAsync();

            if (_logicHomeMode != null)
                await _logicHomeMode.TickAsync();

            if ((DateTime.UtcNow - State.LastKeepAliveReceivedTime).TotalSeconds > 35)
            {
                await OnDisconnectedAsync(DateTime.UtcNow, false);
                return;
            }

            try
            {
                if (State.BattleId != null)
                {
                    var battleServiceGrain = GrainHelper.GetBattleGrain(GrainFactory, State.BattleId.Value);

                    var r = await battleServiceGrain.IsBattleActiveWithPlayersAsync([State.AccountId]);

                    if (r.Item3 > 0)
                        await SendBattleServerErrorAsync(r.Item3, false);
                    else if (!r.Item1 || !r.Item2)
                        if (++_battleProblemCounter > 1)
                            await SendBattleServerErrorAsync(38, false);
                }
            }
            catch
            {
                if (++_battleProblemCounter > 1)
                    await SendBattleServerErrorAsync(39, false);
            }

            if (State.HomeCreatedTime != default)
                await WriteStateAsync();
        }
        catch (Exception e)
        {
            var error = e.ToString();

            Logger.Error(error);

            if (_messageManager != null)
                await _messageManager.SendMessagesAndDisconnectAsync(new LoginFailedMessage
                {
                    Capacity = 300,
                    ErrorCode = 1,
                    Reason = error
                });
        }
    }

    public ValueTask<bool> BuildNewAccount(long loginMessageAccountId)
    {
        if (State.HomeCreatedTime != default)
            return ValueTask.FromResult(false);

        State.AccountId = loginMessageAccountId;

        State.HomeId = 0;
        State.AvatarName = "Brawler";
        State.LobbyTheme = HomeSettings.GetConfig().Decorations.DefaultLobbyThemeGid;

        State.HomeCreatedTime = DateTime.UtcNow;

        State.Gold = HomeSettings.GetConfig().Resources.StartingGold;
        State.Diamonds = HomeSettings.GetConfig().Resources.StartingGems;
        State.StarPoints = HomeSettings.GetConfig().Resources.StartingStarPoints;
        State.Tickets = HomeSettings.GetConfig().Resources.StartingTickets;

        State.Region = "RU";
        State.AvailableBattleTokens = 200;
        State.ThumbnailGlobalId = GlobalId.CreateGlobalId(28, 0);
        State.NameColorGlobalId = GlobalId.CreateGlobalId(43, 0);

        State.ForcedDrops.PityCounters = new int[5];
        State.ForcedDrops.PityCounters[0] = HomeSettings.GetConfig().GachaSystem.NewAccountPityCounterForRareBrawlers;
        State.ForcedDrops.PityCounters[1] =
            HomeSettings.GetConfig().GachaSystem.NewAccountPityCounterForSuperRareBrawlers;
        State.ForcedDrops.PityCounters[2] = HomeSettings.GetConfig().GachaSystem.DefaultPityCounterForEpicBrawlers;
        State.ForcedDrops.PityCounters[3] = HomeSettings.GetConfig().GachaSystem.DefaultPityCounterForMythicBrawlers;
        State.ForcedDrops.PityCounters[4] = HomeSettings.GetConfig().GachaSystem.DefaultPityCounterForLegendaryBrawlers;

        State.StarPowerPityCounter = HomeSettings.GetConfig().GachaSystem.DefaultPityCounterForStarPower;

        State.RareBrawlerChance = HomeSettings.GetConfig().GachaSystem.DefaultRareBrawlerChance;
        State.SuperRareBrawlerChance = HomeSettings.GetConfig().GachaSystem.DefaultSuperRareBrawlerChance;
        State.EpicBrawlerChance = HomeSettings.GetConfig().GachaSystem.DefaultEpicBrawlerChance;
        State.MythicBrawlerChance = HomeSettings.GetConfig().GachaSystem.DefaultMythicBrawlerChance;
        State.LegendaryBrawlerChance = HomeSettings.GetConfig().GachaSystem.DefaultLegendaryBrawlerChance;
        State.StarPowerChance = HomeSettings.GetConfig().GachaSystem.DefaultStarPowerChance;

        var hids = HomeSettings.GetConfig().Brawlers.StartingHeroGIds;
        foreach (var brawler in hids)
            State.HeroEntries.Add(new HeroEntry(brawler) { CharacterState = 0 });

        State.HomeBrawlerGlobalId = hids.Length > 0 ? hids[0] : 16000000;

        State.EventSlots.Add(new EventSlot { Slot = 1, Unlocked = true });
        State.EventSlots.Add(new EventSlot { Slot = 2 });
        State.EventSlots.Add(new EventSlot { Slot = 3 });
        State.EventSlots.Add(new EventSlot { Slot = 4 });
        State.EventSlots.Add(new EventSlot { Slot = 5 });
        State.EventSlots.Add(new EventSlot { Slot = 6, Unlocked = true });
        State.EventSlots.Add(new EventSlot { Slot = 7 });
        State.EventSlots.Add(new EventSlot { Slot = 8 });

        State.Events =
        [
            new EventData
            {
                Slot = 1, Id = int.MaxValue - 1,
                LocationGlobalId = GlobalId.CreateGlobalId(15, 7), EndTime = DateTime.UtcNow.AddSeconds(10),
                MiniBoxReward = 10
            }
        ];

        State.NameSetByUser = false;

        State.TutorialState = HomeSettings.GetConfig().Progress.DefaultTutorialState;
        State.NowTrophies = HomeSettings.GetConfig().Progress.StartingTrophies;
        State.MaxTrophies = HomeSettings.GetConfig().Progress.StartingTrophies;

        State.HeroEntries[0].Trophies = State.NowTrophies;
        State.HeroEntries[0].MaxTrophies = State.MaxTrophies;

        State.Experience = HomeSettings.GetConfig().Progress.StartingExperience;
        State.TrophyRoadProgress = 0;

        State.MiniBoxTokens = 100 * HomeSettings.GetConfig().Boxes.StartingMiniBoxesCount;
        State.BigBoxStarTokens = 10 * HomeSettings.GetConfig().Boxes.StartingBigBoxesCount;

        return ValueTask.FromResult(true);
    }

    public async ValueTask<bool> AddNotifications(byte[][] datas)
    {
        foreach (var d in datas)
        {
            var notif = MessagePackSerializer.Deserialize<BaseNotification>(d);

            var v = State.Notifications.TryAdd(notif.NotificationIndex, notif);
            if (!v) return false;

            if (_commandManager != null)
                await _commandManager.SendCommandsAsync(new LogicAddNotificationCommand { Notification = notif });
        }

        return true;
    }

    public ValueTask KeepAliveAsync()
    {
        State.LastKeepAliveReceivedTime = DateTime.UtcNow;
        return ValueTask.CompletedTask;
    }

    public Task<int> GetNowTrophies()
    {
        return Task.FromResult(State.NowTrophies);
    }

    public Task<(long, string)> GetAllianceIdAndAvatarName()
    {
        return Task.FromResult((State.AllianceId, State.AvatarName));
    }

    public Task RegisterBridgeObserverAsync(IBridgeObserver observer)
    {
        _bridgeObserver = observer;
        return Task.CompletedTask;
    }

    public Task UnregisterBridgeObserverAsync(IBridgeObserver observer)
    {
        if (_bridgeObserver?.Equals(observer) == true)
            _bridgeObserver = null;

        return Task.CompletedTask;
    }

    public async Task SendBridgeEventToClientAsync(BridgeEvent bridgeEvent)
    {
        if (_bridgeObserver != null)
            try
            {
                await _bridgeObserver.SendBridgeEventAsync(bridgeEvent);
            }
            catch (Exception ex)
            {
                _bridgeObserver = null;

                Logger.Warn($"Failed to send bridge event to client: {ex.Message}");
                throw;
            }
    }

    public ValueTask<DateTime> GetHomeCreatedTime()
    {
        return ValueTask.FromResult(State.HomeCreatedTime);
    }

    public async Task<bool> BattleEndAsync(int brawlerId, int eventId, bool win, int trophiesResult,
        int experienceResult,
        int miniBoxTokensResult, int starPointsResult, int winType, int winData)
    {
        var hero = State.HeroEntries.FirstOrDefault(x => x?.CharacterGlobalId == brawlerId, null);

        if (hero == null)
            return false;

        hero.Trophies += trophiesResult;
        hero.MaxTrophies = Math.Max(hero.MaxTrophies, hero.Trophies);

        State.NowTrophies = State.HeroEntries.AsValueEnumerable().Sum(x => x.Trophies);
        State.MaxTrophies = Math.Max(State.MaxTrophies, State.NowTrophies);

        var eventData = State.Events.FirstOrDefault(x => x.Id == eventId);

        if (eventData != null && win)
            if (!eventData.FirstWinRewardClaimed)
            {
                eventData.FirstWinRewardClaimed = true;

                State.BigBoxStarTokens++;
                State.BigBoxOhdTokens++;
            }

        State.Experience += experienceResult;
        State.StarPoints += starPointsResult;

        var miniBoxTokens = Math.Min(State.AvailableBattleTokens, miniBoxTokensResult);

        State.AvailableBattleTokens -= miniBoxTokens;
        State.MiniBoxTokens += miniBoxTokens;

        State.MiniBoxOhdTokens += miniBoxTokens;
        State.TrophiesOhd += trophiesResult;
        State.LegendaryTrophiesOhd += starPointsResult;

        if (win)
            switch (winType)
            {
                case 1:
                {
                    State.SoloWins++;
                    break;
                }
                case 2:
                {
                    State.DuoWins++;
                    break;
                }
                case 3:
                {
                    State.TrioWins++;
                    break;
                }
                case 4:
                {
                    State.BestRoboRumbleTime = Math.Max(State.BestRoboRumbleTime, winData);
                    break;
                }
                case 5:
                {
                    State.BestBigGameBossTime = Math.Max(State.BestBigGameBossTime, winData);
                    break;
                }
                case 6:
                {
                    State.BestRaidBossLevel = Math.Max(State.BestRaidBossLevel, winData);
                    break;
                }
            }

        State.BattleId = null;

        if (_messageManager != null)
        {
            _messageManager.BattleServerIp = null;
            _messageManager.BattleServerPort = 0;

            _messageManager.BattleServerSessionLow = 0;
            _messageManager.BattleServerSessionHigh = 0;

            _messageManager.BattleIdSpectate = null;
        }

        await WriteStateAsync();

        // ReSharper disable once InvertIf
        if (State.TeamId > 0)
        {
            var data = new TeamMemberData
            {
                DisplayData = new PlayerDisplayData
                {
                    AvatarName = State.AvatarName,
                    Experience = State.Experience,
                    Thumbnail = State.ThumbnailGlobalId,
                    NameColor = State.NameColorGlobalId
                },

                CharacterGlobalId = hero.CharacterGlobalId,
                SkinGlobalId = State.SelectedSkins.FirstOrDefault(x =>
                    LogicHomeMode.SkinIdToCharacterData[x].GlobalId == hero.CharacterGlobalId),

                HeroTrophies = hero.Trophies,
                HeroMaxTrophies = hero.MaxTrophies,
                HeroPowerLevel = hero.PowerLevel,

                StarPowerGlobalId = hero.StarPowersContainer.FirstOrDefault(x => x.Value).Key,

                DifficultyLevel =
                    State.Events.FirstOrDefault(x => x.TicketEventDifficulty > 0)?.TicketEventDifficulty ??
                    0
            };

            await GrainHelper.GetTeamGrain(GrainFactory, State.TeamId).SetMemberDataAsync(State.AccountId, data);
        }

        // ReSharper disable once InvertIf
        if (State.AllianceId > 0)
        {
            var alliance = GrainHelper.GetAllianceGrain(GrainFactory, State.AllianceId);
            await alliance.ChangeMemberInfoAsync(State.AccountId, GetAllianceDetailedHomeModel().Result);
        }

        await UpdateMyFriendEntryInFriendshipAsync();

        _ = LeaderboardContainer.LeaderboardService.UpdatePlayerTrophiesAsync(State.AccountId,
            State.NowTrophies, hero.CharacterGlobalId, hero.Trophies, State.Region);

        return true;
    }

    public async Task<PlayerBrawlerRankingData?> GetMyBrawlerRankingDataAsync(int characterId)
    {
        if (State.HomeCreatedTime == default)
            return null;

        if (State.HeroEntries.FirstOrDefault(x => x.CharacterGlobalId == characterId) == null)
            return null;

        var allianceName = string.Empty;

        try
        {
            if (State.AllianceId > 0)
            {
                var alliance = GrainHelper.GetAllianceGrain(GrainFactory, State.AllianceId);
                allianceName = (await alliance.GetAllianceInfoAsync()).Name;
            }
        }
        catch
        {
            // ignored;
        }

        return new PlayerBrawlerRankingData
        {
            AccountId = State.AccountId,

            BrawlerGlobalId = characterId,
            BrawlerTrophies = State.HeroEntries.FirstOrDefault(x => x.CharacterGlobalId == characterId)?.Trophies ?? 0,

            DisplayData = new PlayerDisplayData
            {
                AvatarName = State.AvatarName,
                Experience = State.Experience,
                Thumbnail = State.ThumbnailGlobalId,
                NameColor = State.NameColorGlobalId
            },

            AllianceName = allianceName
        };
    }

    public async Task<PlayerRankingData?> GetMyRankingDataAsync()
    {
        if (State.HomeCreatedTime == default)
            return null;

        var allianceName = string.Empty;

        try
        {
            if (State.AllianceId > 0)
            {
                var alliance = GrainHelper.GetAllianceGrain(GrainFactory, State.AllianceId);
                allianceName = (await alliance.GetAllianceInfoAsync()).Name;
            }
        }
        catch
        {
            // ignored;
        }

        return new PlayerRankingData
        {
            AccountId = State.AccountId,

            Trophies = State.NowTrophies,

            DisplayData = new PlayerDisplayData
            {
                AvatarName = State.AvatarName,
                Experience = State.Experience,
                Thumbnail = State.ThumbnailGlobalId,
                NameColor = State.NameColorGlobalId
            },

            AllianceName = allianceName
        };
    }

    public async Task StopPlayersSearchAsync()
    {
        if (_messageManager == null)
            return;

        await _messageManager.StopPlayersSearchAsync();
    }

    public async Task SendMatchmakeStatusAsync(Guid id, Dictionary<long, long> players, int maxPlayers, int fs)
    {
        if (_messageManager == null)
            return;

        await _messageManager.SendMessagesAsync(new MatchMakingStatusMessage
        {
            Found = players.Count, ShowTips = true, Max = maxPlayers, Seconds = fs
        });
    }

    public async Task KickFromMatchmakingAsync(Guid id, int reason)
    {
        if (_messageManager != null)
        {
            _messageManager.MatchmakeId = null;
            _messageManager.MatchmakingServiceGrain = null;

            await _messageManager.SendMessagesAsync(new MatchmakeFailedMessage { ErrorCode = reason });
        }

        if (State.TeamId <= 0)
            return;

        var team = GrainHelper.GetTeamGrain(GrainFactory, State.TeamId);
        _ = team.CancelMatchmakingAsync(false);
    }

    public async Task ToBattleFromMatchmakingAsync(Guid id)
    {
        if (_messageManager != null)
        {
            _messageManager.MatchmakeId = null;
            _messageManager.MatchmakingServiceGrain = null;
        }

        if (State.TeamId <= 0)
            return;

        var team = GrainHelper.GetTeamGrain(GrainFactory, State.TeamId);
        await team.ToBattleFromMatchmakingAsync(id);
    }

    public ValueTask<Guid?> GetMyBattleServiceGrainId()
    {
        return ValueTask.FromResult(State.BattleId);
    }

    public async Task SendBattleServerErrorAsync(int error, bool writeState = true)
    {
        if (_messageManager != null)
            await _messageManager.SendMessagesAsync(new ServerErrorMessage { Error = error });

        State.BattleId = null;

        if (State.HomeCreatedTime != default && writeState)
            await WriteStateAsync();

        _battleProblemCounter = 0;
    }

    public async ValueTask<LogicPlayer?> SetBattleAsync(Guid battleServiceGrainKey)
    {
        if (State.BattleId != null)
            return null;

        State.BattleId = battleServiceGrainKey;

        var selectedHero = State.HeroEntries.First(x => x.CharacterGlobalId == State.HomeBrawlerGlobalId);
        var selectedSkin = 0;
        var starPower = 0;

        if (selectedHero.StarPowersContainer.Count > 0)
            starPower = selectedHero.StarPowersContainer.First(x => x.Value).Key;

        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var s in State.SelectedSkins.ToList())
        {
            var sCharacter = LogicHomeMode.SkinIdToCharacterData.GetValueOrDefault(s);

            if (sCharacter == null)
                continue;

            if (sCharacter.GlobalId != selectedHero.CharacterGlobalId)
                continue;

            selectedSkin = s;
            break;
        }

        await WriteStateAsync();

        return new LogicPlayer
        {
            AccountId = State.AccountId,
            PlayerIndex = 0,
            TeamIndex = 0,
            Unk1 = 0,
            Unk2 = 0,
            CharacterGlobalId = selectedHero.CharacterGlobalId,
            SkinGlobalId = selectedSkin,
            HeroUpgrades = starPower > 0 ? new LogicHeroUpgrades { StarPowerGlobalId = starPower } : null,
            DisplayData = new PlayerDisplayData
            {
                AvatarName = State.AvatarName,
                Experience = State.Experience,
                Thumbnail = State.ThumbnailGlobalId,
                NameColor = State.NameColorGlobalId
            }
        };
    }

    private async Task<bool> CreateMyBattleLoadingAsync()
    {
        if (State.BattleId == null)
            return false;

        if (_messageManager == null)
            return false;

        var battle = GrainHelper.GetBattleGrain(GrainFactory, State.BattleId.Value);

        var data = await battle.GetMyPlayerLoadingData(State.AccountId,
            HomeSettings.GetConfig().Decorations.ShowBattleGameHints, _messageManager.B);

        if (data == null)
        {
            State.BattleId = null;

            if (State.HomeCreatedTime != default)
                await WriteStateAsync();

            return false;
        }

        var d = data.Value;

        _messageManager.BattleServerIp = d.ip;
        _messageManager.BattleServerPort = d.port;

        _messageManager.BattleServerSessionLow = d.lowSessionId;
        _messageManager.BattleServerSessionHigh = d.highSessionId;

        _messageManager.KaNaN = d.kanan;

        if (d.startLoadingMessage != null)
            return await SendMessagesToPlayerAsync([d.startLoadingMessage.Value]);

        return false;
    }

    private async ValueTask CorrectSupportedContentCreator()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(State.SupportedContentCreator)) return;

            var cg = GrainFactory.GetGrain<IContentCreatorRewardServiceGrain>(State.SupportedContentCreator);

            if (await cg.IsActivated()) return;

            await cg.RemoveSupporter(State.AccountId);
            State.SupportedContentCreator = string.Empty;
        }
        catch (Exception e)
        {
            Logger.Warn(e);
        }
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var accountId = this.GetPrimaryKeyString().Split('_').Last();
        State.AccountId = Convert.ToInt64(accountId);

        return base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        State.GameState = 0;
        State.SessionId = Guid.Empty;

        _tickTimer?.Dispose();
        _tickTimer = null;

        _battleProblemCounter = 0;

        if (_messageManager != null)
            await _messageManager.GoodbyeAsync();
        _messageManager = null;

        if (_commandManager != null)
            await _commandManager.GoodbyeAsync();
        _commandManager = null;

        if (_logicHomeMode != null)
            await _logicHomeMode.GoodbyeAsync();
        _logicHomeMode = null;

        if (State.HomeCreatedTime != default)
            await WriteStateAsync();

        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}