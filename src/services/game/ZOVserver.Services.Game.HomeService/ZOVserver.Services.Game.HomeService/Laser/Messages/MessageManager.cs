using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using MessagePack;
using NLog;
using ZLinq;
using ZOVserver.Services.Game.HomeService.Alliance;
using ZOVserver.Services.Game.HomeService.Laser.Commands;
using ZOVserver.Services.Game.HomeService.Laser.Mode;
using ZOVserver.Services.Game.HomeService.Leaderboard;
using ZOVserver.Services.Game.HomeService.Manager;
using ZOVserver.Services.Game.HomeService.States;
using ZOVserver.Services.Game.HomeService.Telemetry;
using ZOVserver.Shared.Abstractions;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Interfaces;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;
using ZOVserver.Shared.Contracts.Laser.Combined.Entries;
using ZOVserver.Shared.Contracts.Laser.Combined.Friend;
using ZOVserver.Shared.Contracts.Laser.Combined.Notifications;
using ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Inheritors;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;
using ZOVserver.Shared.Contracts.Laser.Combined.Stream;
using ZOVserver.Shared.Contracts.Laser.Combined.Team;
using ZOVserver.Shared.Contracts.Laser.Commands.ToClient;
using ZOVserver.Shared.Contracts.Laser.DebugInfo;
using ZOVserver.Shared.Contracts.Laser.Machine;
using ZOVserver.Shared.Contracts.Laser.Messages;
using ZOVserver.Shared.Contracts.Laser.Messages.Client;
using ZOVserver.Shared.Contracts.Laser.Messages.Server;
using ZOVserver.Shared.Contracts.Models;
using ZOVserver.Shared.Contracts.Structs;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;
using ZOVserver.Shared.TitanRemnants.Utility;

namespace ZOVserver.Services.Game.HomeService.Laser.Messages;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedParameter.Local")]
internal class MessageManager(
    IHomeServiceGrain grain,
    HomeState state,
    IPlayerSessionServiceGrain playerSession,
    IGrainFactory grainFactory,
    ITeamPlayersSearchService teamPlayersSearchService)
    : IMessageManager
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly FrozenDictionary<int, MessageProcessorDelegate> Processors;
    private static readonly IReadOnlyList<(int messageType, string messageName)> ImplMessages;

    internal readonly ConcurrentDictionary<long, DateTime> MutedPlayers = new();

    internal readonly HashSet<long> TeamIdsWithMyTrash = [];
    internal readonly ConcurrentDictionary<long, FriendEntry> TeamInvites = new();
    internal readonly HashSet<long> TeamRequests = [];

    private bool _inTeamSearchPlayer;

    private string? _playerTeamSearchBucket;
    private int _playerTeamSearchTick;
    private int _teamStatus;

    private int _v101;
    private int _v102;
    private bool _v103;

    static MessageManager()
    {
        var methods = typeof(MessageManager).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
        var cd = new Dictionary<int, MessageProcessorDelegate>();
        var debugList = new List<(int, string)>();

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length < 1 || !parameters[0].ParameterType.IsSubclassOf(typeof(PiranhaMessage))) continue;

            var message = parameters[0].ParameterType;
            var instance = (PiranhaMessage)Activator.CreateInstance(message)!;
            var msgId = instance.GetMessageType();

            cd.Add(msgId, CompileProcessor(method, message));

            var debugName = DebugInfoCollector.PacketCollectorY.GetValueOrDefault(msgId, ("UNKNOWN-NAME", -1)).Item1;
            debugList.Add((msgId, debugName));
        }

        Processors = cd.ToFrozenDictionary();
        ImplMessages = debugList.ToImmutableList();
    }

    internal IMatchmakingServiceGrain? MatchmakingServiceGrain { get; set; }
    internal Guid? MatchmakeId { get; set; }

    internal byte B { get; set; }

    internal string? BattleServerIp { get; set; }
    internal int BattleServerPort { get; set; }

    internal ulong BattleServerSessionLow { get; set; }
    internal ushort BattleServerSessionHigh { get; set; }

    internal byte[] KaNaN { get; set; }

    internal Guid? BattleIdSpectate { get; set; }

    public CommandManager? CommandManager { get; set; }
    public LogicHomeMode? HomeMode { get; set; }

    public static IReadOnlyList<(int messageType, string messageName)> GetAllImplementedMessages()
    {
        return ImplMessages;
    }

    public async Task<int> ReceiveMessageAsync(PiranhaMessage piranhaMessage)
    {
        if (state.GameState == 0) return -5999;
        if (!Processors.TryGetValue(piranhaMessage.GetMessageType(), out var processor)) return -6000;

        try
        {
            return await processor(this, piranhaMessage);
        }
        catch (Exception e)
        {
            var error = e.ToString();
            Logger.Error(error);

            return await SendMessagesAndDisconnectAsync(new LoginFailedMessage
                { Capacity = 300, ErrorCode = 1, Reason = error })
                ? 0
                : -6003;
        }
    }

    public async Task<bool> SendMessagesAsync(params PiranhaMessage[] piranhaMessage)
    {
        if (state.GameState == 0 || state.SessionId == Guid.Empty)
            return false;

        var msgs = new List<PiranhaMessageStruct>();
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var message in piranhaMessage)
            {
                var messageStruct = LaserContractSerializer.SerializeToStruct(message);

                msgs.Add(messageStruct);
            }
        }

        await grain.SendMessagesToPlayerAsync(msgs.ToArray());
        return true;
    }

    public async Task<bool> SendMessagesAndDisconnectAsync(params PiranhaMessage[] piranhaMessage)
    {
        if (state.GameState == 0 || state.SessionId == Guid.Empty)
            return false;

        var msgs = new List<PiranhaMessageStruct>();
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var message in piranhaMessage)
            {
                var messageStruct = LaserContractSerializer.SerializeToStruct(message);

                msgs.Add(messageStruct);
            }
        }

        await playerSession.DisconnectPlayerWithMessagesAsync(msgs.ToArray());
        return true;
    }

    public async Task TickAsync()
    {
        if (EventsManager.GetMaintenanceSecondsLeft() > 0 && !_v103)
            _v103 = await SendMessagesAsync(new ShutdownStartedMessage());

        foreach (var mp in MutedPlayers.ToArray())
            if (DateTime.UtcNow > mp.Value)
                MutedPlayers.TryRemove(mp.Key, out _);

        if (++_v102 == 2)
        {
            await grain.UpdateMyFriendEntryInFriendshipAsync();
            await grain.UpdateMyFriendOnlineStatusEntryInFriendshipAsync();
        }

        switch (state.TeamId)
        {
            case 0:
                state.TeamEventSlot = 0;
                break;
            case > 0:
            {
                if (_v101 == 0)
                    _teamStatus = 3;

                if (_v101++ % 20 == 0)
                    try
                    {
                        var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);
                        var r = await team.TrySetMemberStatusAsync(state.AccountId, _teamStatus);

                        if (!r)
                            state.TeamId = 0;
                    }
                    catch
                    {
                        state.TeamId = 0;
                    }

                break;
            }
        }

        if (state.TeamId > 0)
        {
            _playerTeamSearchBucket = null;
            _playerTeamSearchTick = 0;
        }

        if (_playerTeamSearchBucket != null)
        {
            var region = LogicDataTables.GetDataByName<LogicRegionData>(state.Region);
            var basic = await grain.GetBasicTeamMemberData();

            if (region == null || basic == null)
                return;

            var memberEntry = new TeamMemberEntry
            {
                IsOwner = false,
                AccountId = state.AccountId,

                CharacterGlobalId = basic.CharacterGlobalId,
                SkinGlobalId = basic.SkinGlobalId,

                HeroTrophies = basic.HeroTrophies,
                HeroMaxTrophies = basic.HeroMaxTrophies,
                HeroPowerLevel = basic.HeroPowerLevel,

                State = 3,
                DifficultyLevel = basic.DifficultyLevel,
                DisplayData = basic.DisplayData,
                StarPowerGlobalId = basic.StarPowerGlobalId
            };

            var radius = _playerTeamSearchTick++ * 500;
            var isGlobal = _playerTeamSearchTick > 30;
            var myRegionId = region.GlobalId;

            var results = await teamPlayersSearchService.GetPotentialTeamsAsync(
                _playerTeamSearchBucket, state.NowTrophies, radius);

            var potentialTeams = results
                .Where(x => isGlobal || x.RegionId == myRegionId)
                .OrderByDescending(x => x.RegionId == myRegionId)
                .ThenBy(x => Math.Abs(x.AvgTrophies - state.NowTrophies))
                .Take(50)
                .ToArray();

            foreach (var team in potentialTeams)
                try
                {
                    var tgrain = GrainHelper.GetTeamGrain(grainFactory, team.TeamId);

                    var joinResult = await tgrain.JoinByTeamPlayersSearchAsync(memberEntry);

                    switch (joinResult)
                    {
                        case -1 or -2:
                            throw new Exception("Invalid team!");
                        case 0:
                        {
                            state.TeamId = team.TeamId;

                            if (state.AllianceId > 0)
                                _ = await tgrain.AddOrChangeMemberAllianceIdAsync(state.AccountId, state.AllianceId);

                            _playerTeamSearchBucket = null;
                            _playerTeamSearchTick = 0;
                            await SendMessagesAsync(new MatchMakingCancelledMessage());

                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Warn($"Room {team.TeamId} is dead. Removing. Error: {e.Message}");

                    if (_playerTeamSearchBucket != null && team.RawValue != null)
                        await teamPlayersSearchService.RemoveTeamFromSearch(_playerTeamSearchBucket, team.RawValue);
                }
        }
    }

    public async ValueTask GoodbyeAsync()
    {
        CommandManager = null;
        HomeMode = null;

        state.PlayerStatus = 0;

        _v101 = 0;
        _v102 = 0;

        _playerTeamSearchBucket = null;
        _playerTeamSearchTick = 0;
        _inTeamSearchPlayer = false;

        BattleServerIp = null;
        BattleServerPort = 0;
        BattleServerSessionLow = 0;
        BattleServerSessionHigh = 0;

        if (BattleIdSpectate != null)
        {
            var battle = GrainHelper.GetBattleGrain(grainFactory, BattleIdSpectate.Value);

            _ = battle.RemoveSpectator(state.AccountId);

            BattleIdSpectate = null;
        }

        if (MatchmakeId != null && MatchmakingServiceGrain != null)
            try
            {
                await MatchmakingServiceGrain.RemovePlayersFromMatchmakingAsync(MatchmakeId.Value, [state.AccountId],
                    true);

                MatchmakingServiceGrain = null;
                MatchmakeId = null;
            }
            catch
            {
                MatchmakingServiceGrain = null;
                MatchmakeId = null;
            }

        if (state.AllianceId > 0)
        {
            var alliance = GrainHelper.GetAllianceGrain(grainFactory, state.AllianceId);
            await alliance.ChangeMemberStatusAsync(state.AccountId, state.PlayerStatus, state.TeamId > 0);
        }

        await grain.UpdateMyFriendOnlineStatusEntryInFriendshipAsync();

        if (state.TeamId > 0)
            try
            {
                var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

                var r = await team.TrySetMemberStatusAsync(state.AccountId, 0);

                if (!r)
                    state.TeamId = 0;
            }
            catch
            {
                state.TeamId = 0;
            }

        foreach (var team in TeamIdsWithMyTrash.Select(id => GrainHelper.GetTeamGrain(grainFactory, id)))
            await team.RemoveAllPlayerPendingActionsAsync(state.AccountId);

        TeamIdsWithMyTrash.Clear();
        TeamInvites.Clear();
        TeamRequests.Clear();
        MutedPlayers.Clear();
    }

    private static MessageProcessorDelegate CompileProcessor(MethodInfo method, Type messageType)
    {
        var managerParam = Expression.Parameter(typeof(MessageManager), "manager");
        var messageParam = Expression.Parameter(typeof(PiranhaMessage), "message");

        var castedMessage = Expression.Convert(messageParam, messageType);
        var methodCall = Expression.Call(managerParam, method, castedMessage);

        if (method.ReturnType == typeof(Task<int>))
            return Expression.Lambda<MessageProcessorDelegate>(methodCall, managerParam, messageParam).Compile();

        if (method.ReturnType == typeof(Task<bool>))
        {
            var bridgeMethod = typeof(MessageManager)
                .GetMethod(nameof(ConvertTaskBoolToTaskInt), BindingFlags.Static | BindingFlags.NonPublic)!;

            var wrappedCall = Expression.Call(bridgeMethod, methodCall);

            return Expression.Lambda<MessageProcessorDelegate>(wrappedCall, managerParam, messageParam).Compile();
        }

        Expression body = method.ReturnType switch
        {
            var t when t == typeof(int) =>
                Expression.Call(typeof(Task), nameof(Task.FromResult), [typeof(int)], methodCall),
            var t when t == typeof(bool) =>
                Expression.Call(typeof(Task), nameof(Task.FromResult), [typeof(int)],
                    Expression.Condition(methodCall, Expression.Constant(0), Expression.Constant(-1))),
            _ =>
                Expression.Call(typeof(Task), nameof(Task.FromResult), [typeof(int)], Expression.Constant(-6001))
        };

        return Expression.Lambda<MessageProcessorDelegate>(body, managerParam, messageParam).Compile();
    }

    private static async Task<int> ConvertTaskBoolToTaskInt(Task<bool> task)
    {
        return await task ? 0 : -1;
    }

    private async Task<int> GoHomeFromOfflinePractiseReceived(GoHomeFromOfflinePractiseMessage message)
    {
        await grain.SendOwnHomeDataAsync(false);
        return 0;
    }

    private async Task<int> EndClientTurnMessageReceived(EndClientTurnMessage endClientTurnMessage)
    {
        if (HomeMode != null)
            return await HomeMode.EndClientTurnReceived(endClientTurnMessage.Tick,
                endClientTurnMessage.Checksum,
                endClientTurnMessage.Commands);
        return -1;
    }

    private async Task<int> AvatarNameCheckRequestMessageReceived(AvatarNameCheckRequestMessage message)
    {
        if (CommandManager == null) return -1;
        if (!state.NameSetByUser) return -2;

        var reason = message.Name.Trim().Length switch
        {
            < 3 => 2,
            > 15 => 1,
            _ => 0
        };

        await SendMessagesAsync(
            new AvatarNameCheckResponseMessage
            {
                Capacity = 16, ReasonIsNotNull = reason != 0, Reason = reason
            });
        return 0;
    }

    private async Task<int> ChangeAvatarNameMessageReceived(ChangeAvatarNameMessage message)
    {
        if (CommandManager == null) return -1;

        message.Name = message.Name.Trim();

        var reason = message.Name.Length switch
        {
            < 3 => 2,
            > 15 => 1,
            _ => 0
        };

        if (reason == 0)
            return await CommandManager.SendCommandsAsync(
                new LogicChangeAvatarNameCommand
                {
                    NewName = message.Name,
                    ChangeNamePrice = state.NextNameChangePrice
                },
                new LogicGemNameChangeStateChangedCommand
                {
                    NextNameChangePrice = -1,
                    NextNameChangeSeconds = -1
                });

        await SendMessagesAsync(
            new AvatarNameChangeFailedMessage
            {
                Capacity = 8,
                Reason = reason
            });

        return 0;
    }

    private async Task<int> SetInvitesBlockedMessageReceived(SetInvitesBlockedMessage piranhaMessage)
    {
        if (CommandManager == null) return -1;

        await CommandManager.SendCommandsAsync(new LogicInviteBlockingChangedCommand { State = piranhaMessage.State });
        return 0;
    }

    private async Task<int> GetSeasonRewardsMessageReceived(GetSeasonRewardsMessage message)
    {
        return await SendMessagesAsync(new SeasonRewardsMessage { Capacity = 512 }) ? 0 : -1;
    }

    private Task<bool> AnalyticEventMessageReceived(AnalyticEventMessage message)
    {
        var eventA = message.AnalyticEvent;

        // ReSharper disable once InvertIf
        if (eventA is { Event: "tutorial_step", EventInfo: "{\"step\":\"click_to_end\",\"step_id\":\"18\"}" })
            state.TutorialState = 1;

        return Task.FromResult(true);
    }

    private async Task<int> SetSupportedCreatorMessageReceived(SetSupportedCreatorMessage message)
    {
        if (CommandManager == null)
            return -1;

        IContentCreatorRewardServiceGrain? cg;

        if (string.IsNullOrWhiteSpace(message.Code))
        {
            if (string.IsNullOrWhiteSpace(state.SupportedContentCreator)) return -2;

            cg = grainFactory.GetGrain<IContentCreatorRewardServiceGrain>(state.SupportedContentCreator);

            await cg.RemoveSupporter(state.AccountId);
            state.SupportedContentCreator = string.Empty;

            return
                await CommandManager.SendCommandsAsync(new LogicSetSupportedCreatorCommand
                    { Code = new SupportedCreatorEntry() });
        }

        cg = grainFactory.GetGrain<IContentCreatorRewardServiceGrain>(message.Code);

        if (!await cg.IsActivated())
        {
            await SendMessagesAsync(
                new SetSupportedCreatorResponseMessage
                {
                    Capacity = 64,
                    ErrorCode = 1,
                    Code = message.Code
                });

            return 0;
        }

        if (!string.IsNullOrWhiteSpace(state.SupportedContentCreator))
        {
            var cgold = grainFactory.GetGrain<IContentCreatorRewardServiceGrain>(state.SupportedContentCreator);

            await cgold.RemoveSupporter(state.AccountId);
            state.SupportedContentCreator = string.Empty;
        }

        await cg.AddSupporter(state.AccountId);
        state.SupportedContentCreator = message.Code;

        return await CommandManager.SendCommandsAsync(new LogicSetSupportedCreatorCommand
            { Code = new SupportedCreatorEntry { Code = message.Code } });
    }

    private async Task<int> AskForBattleEndMessageReceived(AskForBattleEndMessage message)
    {
        state.TutorialState = 2;

        await SendMessagesAsync(new BattleEndMessage { Capacity = 512 });

        return 0;
    }

    private async Task<bool> PlayerStatusMessageReceived(PlayerStatusMessage message)
    {
        state.PlayerStatus = message.Status;

        // ReSharper disable once InvertIf
        if (state.AllianceId > 0)
        {
            var alliance = GrainHelper.GetAllianceGrain(grainFactory, state.AllianceId);
            await alliance.ChangeMemberStatusAsync(state.AccountId, state.PlayerStatus, state.TeamId > 0);
        }

        await grain.UpdateMyFriendOnlineStatusEntryInFriendshipAsync();

        return true;
    }

    private async Task<int> SetCountryMessageReceived(SetCountryMessage message)
    {
        var region = LogicDataTables.GetDataById<LogicRegionData>(message.Country);

        if (region == null)
            return -1;

        if (!region.IsCountry)
            return -2;

        if (DateTime.UtcNow - state.LastCountryChangeTime < TimeSpan.FromDays(1))
            return await SendMessagesAsync(new SetCountryResponseMessage
            {
                Response = 1,
                Country = region.GlobalId
            })
                ? 0
                : -3;

        var oldRegion = state.Region;
        var oldLastChangeTime = state.LastCountryChangeTime;

        try
        {
            state.Region = region.Name;
            state.LastCountryChangeTime = DateTime.UtcNow;

            await grain.SaveHomeStateAsync();
        }
        catch
        {
            state.Region = oldRegion;
            state.LastCountryChangeTime = oldLastChangeTime;

            return -4;
        }

        try
        {
            await LeaderboardContainer.LeaderboardService.UpdatePlayerFullDataAsync(state.AccountId,
                oldRegion, state.Region,
                state.HeroEntries.ToArray().ToDictionary(x => x.CharacterGlobalId, y => y.Trophies));
        }
        catch
        {
            state.Region = oldRegion;
            state.LastCountryChangeTime = oldLastChangeTime;

            try
            {
                await grain.SaveHomeStateAsync();
            }
            catch
            {
                return -5;
            }

            return -6;
        }

        return await SendMessagesAsync(new SetCountryResponseMessage
        {
            Response = 0,
            Country = region.GlobalId
        })
            ? 0
            : -7;
    }

    private async Task<bool> CreateAllianceMessageReceived(CreateAllianceMessage message)
    {
        try
        {
            if (message.Name.Length is > 15 or < 2)
                return false;

            if (message.Description.Length > 256)
                return false;

            if (message.AllianceType is not (1 or 2 or 3))
                return false;

            if (LogicDataTables.GetDataById<LogicRegionData>(message.RegionGlobalId) == null)
                return false;

            if (LogicDataTables.GetDataById<LogicAllianceBadgeData>(message.BadgeGlobalId) == null)
                return false;

            var lang = await playerSession.GetPlayerLocalizationGlobalId();

            var allianceParams = new AllianceParams
            {
                AllianceId = GenerateAllianceId(),
                Name = message.Name,
                Description = message.Description,
                OwnerAccountId = state.AccountId,
                AllianceType = message.AllianceType,
                RegionGlobalId = message.RegionGlobalId,
                LanguageGlobalId = lang,
                BadgeGlobalId = message.BadgeGlobalId,
                RequiredTrophies = message.RequiredTrophies
            };

            var allianceMember = new AllianceMember
            {
                AccountId = state.AccountId,
                JoinTime = DateTime.UtcNow,
                Role = AllianceRole.Leader
            };

            var alliance = GrainHelper.GetAllianceGrain(grainFactory, allianceParams.AllianceId);
            var res = await alliance.CreateAllianceAsync(allianceParams, allianceMember);

            if (res == null)
                return await SendMessagesAsync(new AllianceResponseMessage { Response = 21 });

            await grain.CorrectAllianceId(allianceParams.AllianceId);

            MetricsClient.ObserveClubRegistration();

            var header = new AllianceHeaderEntry
            {
                AllianceId = allianceParams.AllianceId,
                AllianceName = allianceParams.Name,
                BadgeGlobalId = allianceParams.BadgeGlobalId,
                AllianceType = allianceParams.AllianceType,
                MembersCount = 1,
                NowTrophies = state.NowTrophies,
                RequiredTrophies = allianceParams.RequiredTrophies,
                PreferredLanguageGlobalId = allianceParams.LanguageGlobalId,
                Region = LogicDataTables.GetDataById(allianceParams.RegionGlobalId)?.Name ?? "RU"
            };

            return await SendMessagesAsync(
                new AllianceResponseMessage { Response = 20 },
                new MyAllianceMessage
                {
                    OnlineMembers = 1,
                    MyAllianceObject = new MyAllianceObject
                    {
                        MyRoleDataRef = 25_000_002,
                        AllianceHeaderEntry = header
                    }
                });
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());

            return await SendMessagesAsync(new AllianceResponseMessage { Response = 21 });
        }

        long GenerateAllianceId()
        {
            return RandomNumberGenerator.GetInt32(int.MaxValue - 1) + 1;
        }
    }

    private async Task<bool> AskForAllianceDataMessageReceived(AskForAllianceDataMessage message)
    {
        var alliance = GrainHelper.GetAllianceGrain(grainFactory, message.AllianceId);

        var data = await alliance.GetDetailedAllianceInfoAsync();

        var header = new AllianceHeaderEntry
        {
            AllianceId = data.AllianceParams.AllianceId,
            AllianceName = data.AllianceParams.Name,
            BadgeGlobalId = data.AllianceParams.BadgeGlobalId,
            AllianceType = data.AllianceParams.AllianceType,
            MembersCount = data.AllianceMembers?.Length ?? 0,
            NowTrophies = data.AllianceMembers?.Sum(x => x.HomeModel.Trophies) ?? 0,
            RequiredTrophies = data.AllianceParams.RequiredTrophies,
            PreferredLanguageGlobalId = data.AllianceParams.LanguageGlobalId,
            Region = LogicDataTables.GetDataById(data.AllianceParams.RegionGlobalId)?.Name ?? "RU"
        };

        if (data.AllianceMembers == null)
            return await SendMessagesAsync(new AllianceDataMessage
            {
                IsMyAlliance = message.AllianceId == state.AllianceId,
                AllianceFullEntry = new AllianceFullEntry
                {
                    AllianceHeaderEntry = header,
                    Description = data.AllianceParams.Description,
                    Members = []
                }
            });

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

        return await SendMessagesAsync(
            new AllianceDataMessage
            {
                IsMyAlliance = message.AllianceId == state.AllianceId,
                AllianceFullEntry = new AllianceFullEntry
                {
                    AllianceHeaderEntry = header,
                    Description = data.AllianceParams.Description,
                    Members = memberEntries
                }
            });
    }

    private async Task<bool> AskForAllianceStreamMessageReceived(AskForAllianceStreamMessage message)
    {
        var alliance = GrainHelper.GetAllianceGrain(grainFactory, state.AllianceId);

        var streamEntriesAsBytes = await alliance.GetStreamEntries();

        var streamEntries = streamEntriesAsBytes.Select(x => MessagePackSerializer.Deserialize<StreamEntry>(x))
            .ToList();

        return await SendMessagesAsync(new AllianceStreamMessage
        {
            StreamEntries = streamEntries
        });
    }

    private async Task<bool> ChangeAllianceSettingsMessageReceived(ChangeAllianceSettingsMessage message)
    {
        if (message.AllianceDescription.Length > 256)
            return false;

        if (message.AllianceType is not (1 or 2 or 3))
            return false;

        if (LogicDataTables.GetDataById<LogicRegionData>(message.AllianceRegion) == null)
            return false;

        if (LogicDataTables.GetDataById<LogicAllianceBadgeData>(message.AllianceBadgeData) == null)
            return false;

        var alliance = GrainHelper.GetAllianceGrain(grainFactory, state.AllianceId);

        var res = await alliance.ChangeAllianceSettingsAsync(state.AccountId, new AllianceSettings
        {
            Description = message.AllianceDescription,
            BadgeGlobalId = message.AllianceBadgeData,
            RegionGlobalId = message.AllianceRegion,
            AllianceType = message.AllianceType,
            RequiredTrophies = message.RequiredScore
        });

        if (res != 0)
            return await SendMessagesAsync(new AllianceResponseMessage { Response = 95 });

        var data = await alliance.GetDetailedAllianceInfoAsync();

        var header = new AllianceHeaderEntry
        {
            AllianceId = data.AllianceParams.AllianceId,
            AllianceName = data.AllianceParams.Name,
            BadgeGlobalId = data.AllianceParams.BadgeGlobalId,
            AllianceType = data.AllianceParams.AllianceType,
            MembersCount = data.AllianceMembers?.Length ?? 0,
            NowTrophies = data.AllianceMembers?.Sum(x => x.HomeModel.Trophies) ?? 0,
            RequiredTrophies = data.AllianceParams.RequiredTrophies,
            PreferredLanguageGlobalId = data.AllianceParams.LanguageGlobalId,
            Region = LogicDataTables.GetDataById(data.AllianceParams.RegionGlobalId)?.Name ?? "RU"
        };

        if (data.AllianceMembers == null)
            return false;

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

        return await SendMessagesAsync(
            new ChangeAllianceSettingsOkMessage
            {
                Alliance = new AllianceFullEntry
                {
                    AllianceHeaderEntry = header,
                    Description = data.AllianceParams.Description,
                    Members = memberEntries
                }
            },
            new AllianceResponseMessage { Response = 10 });
    }

    private async Task<bool> JoinAllianceMessageReceived(JoinAllianceMessage message)
    {
        if (state.AllianceId > 0)
            return await SendMessagesAsync(new AllianceResponseMessage { Response = 44 });

        var alliance = GrainHelper.GetAllianceGrain(grainFactory, message.AllianceId);

        var res = await alliance.JoinMemberAsync(
            new AllianceMember { AccountId = state.AccountId, JoinTime = DateTime.UtcNow, Role = AllianceRole.Member },
            await grain.GetAllianceDetailedHomeModel());

        return res switch
        {
            -100 or -101 or -1 => await SendMessagesAsync(new AllianceResponseMessage { Response = 93 }),
            -2 or -3 => await SendMessagesAsync(new AllianceResponseMessage { Response = 43 }),
            -4 => await SendMessagesAsync(new AllianceResponseMessage { Response = 45 }),
            -5 => await SendMessagesAsync(new AllianceResponseMessage { Response = 42 }),
            -6 => await SendMessagesAsync(new AllianceResponseMessage { Response = 46 }),
            -7 => await SendMessagesAsync(new AllianceResponseMessage { Response = 44 }),
            _ => await HandleSuccessfulJoinAsync()
        };

        async Task<bool> HandleSuccessfulJoinAsync()
        {
            await grain.CorrectAllianceId(message.AllianceId);

            var data = await alliance.GetAllianceInfoAndRoleAsync(state.AccountId);

            var streamEntriesAsBytes = await alliance.GetStreamEntries();

            var streamEntries = streamEntriesAsBytes
                .Select(x => MessagePackSerializer.Deserialize<StreamEntry>(x))
                .ToList();

            await SendMessagesAsync(new AllianceResponseMessage { Response = 40 },
                new MyAllianceMessage
                {
                    OnlineMembers = 1,
                    MyAllianceObject = new MyAllianceObject
                    {
                        MyRoleDataRef = (data.Item2 ?? 0) + 25_000_000,
                        AllianceHeaderEntry = new AllianceHeaderEntry
                        {
                            AllianceId = data.Item1.Item1.AllianceId,
                            AllianceName = data.Item1.Item1.Name,
                            BadgeGlobalId = data.Item1.Item1.BadgeGlobalId,
                            AllianceType = data.Item1.Item1.AllianceType,
                            MembersCount = data.Item1.Item2,
                            NowTrophies = data.Item1.Item3,
                            RequiredTrophies = data.Item1.Item1.RequiredTrophies,
                            PreferredLanguageGlobalId = data.Item1.Item1.LanguageGlobalId,
                            Region = LogicDataTables.GetDataById(data.Item1.Item1.RegionGlobalId)?.Name ?? "RU"
                        }
                    }
                },
                new AllianceStreamMessage
                {
                    StreamEntries = streamEntries
                });

            return true;
        }
    }

    private async Task<bool> LeaveAllianceMessageReceived(LeaveAllianceMessage message)
    {
        var alliance = GrainHelper.GetAllianceGrain(grainFactory, state.AllianceId);

        var res = await alliance.LeaveMemberAsync(state.AccountId, state.AvatarName);

        await grain.CorrectAllianceId(0);

        return await SendMessagesAsync(new AllianceResponseMessage { Response = res != 0 ? 93 : 80 },
            new MyAllianceMessage());
    }

    private async Task<bool> ChangeAllianceMemberRoleMessageReceived(ChangeAllianceMemberRoleMessage message)
    {
        var alliance = GrainHelper.GetAllianceGrain(grainFactory, state.AllianceId);

        var res = await alliance.ChangeMemberRoleAsync(state.AccountId, message.MemberId, (AllianceRole)message.NewRole,
            await grain.GetAllianceDetailedHomeModel());

        return res switch
        {
            -1 or -2 or -3 => await SendMessagesAsync(new AllianceResponseMessage { Response = 93 }),
            -4 => await SendMessagesAsync(new AllianceResponseMessage { Response = 94 }),
            -5 => await SendMessagesAsync(new AllianceResponseMessage { Response = 95 }),
            -6 => await SendMessagesAsync(new AllianceResponseMessage { Response = 93 }),
            -7 => await SendMessagesAsync(new AllianceResponseMessage { Response = 95 }),
            _ => await HandleSuccessfulChangeRoleAsync(message.MemberId, res)
        };

        async Task<bool> HandleSuccessfulChangeRoleAsync(long targetId, int type)
        {
            var targetSession = GrainHelper.GetPlayerSession(grainFactory, targetId);

            await targetSession.SendMessagesToPlayerAsync([
                LaserContractSerializer.SerializeToStruct(new AllianceResponseMessage { Response = type + 20 })
            ]);

            return await SendMessagesAsync(new AllianceResponseMessage { Response = type });
        }
    }

    private async Task<bool> KickAllianceMemberMessageReceived(KickAllianceMemberMessage message)
    {
        var alliance = GrainHelper.GetAllianceGrain(grainFactory, state.AllianceId);

        var res = await alliance.KickMemberAsync(state.AccountId, state.AvatarName, message.MemberId);

        return res switch
        {
            -1 or -2 => await SendMessagesAsync(new AllianceResponseMessage { Response = 93 }),
            -3 => await SendMessagesAsync(new AllianceResponseMessage { Response = 94 }),
            -4 => await SendMessagesAsync(new AllianceResponseMessage { Response = 95 }),
            -5 => await SendMessagesAsync(new AllianceResponseMessage { Response = 71 }),
            -6 => await SendMessagesAsync(new AllianceResponseMessage { Response = 93 }),
            _ => await HandleSuccessfulKickAsync(message.MemberId)
        };

        async Task<bool> HandleSuccessfulKickAsync(long targetId)
        {
            var targetSession = GrainHelper.GetPlayerSession(grainFactory, targetId);
            var targetHome = GrainHelper.GetHomeGrain(grainFactory, targetId);

            await targetSession.SendMessagesToPlayerAsync([
                LaserContractSerializer.SerializeToStruct(new AllianceResponseMessage { Response = 100 }),
                LaserContractSerializer.SerializeToStruct(new MyAllianceMessage())
            ]);

            _ = targetHome.KickFromAlliance();

            return await SendMessagesAsync(new AllianceResponseMessage { Response = 70 });
        }
    }

    private async Task<bool> ChatToAllianceStreamMessageReceived(ChatToAllianceStreamMessage message)
    {
        if (message.Message.Length > 128)
            return false;

        var alliance = GrainHelper.GetAllianceGrain(grainFactory, state.AllianceId);

        await alliance.SendTextStreamEntryAsync(state.AccountId, state.AvatarName, message.Message);

        return true;
    }

    private async Task<bool> SendAllianceMailMessageReceived(SendAllianceMailMessage message)
    {
        switch (message.Message.Length)
        {
            case > 256:
                return false;
            case 0:
                return await SendMessagesAsync(new AllianceResponseMessage { Response = 114 });
        }

        var alliance = GrainHelper.GetAllianceGrain(grainFactory, state.AllianceId);

        var res = await alliance.GetMembersAsync();

        if (!res.TryGetValue(state.AccountId, out var my))
            return await SendMessagesAsync(new AllianceResponseMessage { Response = 93 });

        if (my.Role is not (AllianceRole.Leader or AllianceRole.CoLeader))
            return await SendMessagesAsync(new AllianceResponseMessage { Response = 95 });

        BaseNotification notif = new BandNotification
        {
            NotificationIndex = GetRandomInt(),
            CreationTime = DateTime.UtcNow,
            Message = message.Message,
            PlayerDisplayData = (await grain.GetAllianceDetailedHomeModel()).DisplayData
        };

        byte[][] snotifs = [MessagePackSerializer.Serialize(notif)];

        foreach (var member in res)
        {
            if (member.Key == state.AccountId)
            {
                _ = grain.AddNotifications(snotifs);
                continue;
            }

            var memberHome = GrainHelper.GetHomeGrain(grainFactory, member.Key);
            _ = memberHome.AddNotifications(snotifs);
        }

        return await SendMessagesAsync(new AllianceResponseMessage { Response = 113 });

        int GetRandomInt()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }
    }

    private async Task<bool> RequestJoinAllianceMessageReceived(RequestJoinAllianceMessage message)
    {
        if (message.RequestText.Length > 256)
            return false;

        if (state.AllianceId > 0)
            return await SendMessagesAsync(new AllianceResponseMessage { Response = 44 });

        var alliance = GrainHelper.GetAllianceGrain(grainFactory, message.AllianceId);

        var res = await alliance.SendJoinRequestAsync(state.AccountId, state.NowTrophies,
            grain.GetAllianceDetailedHomeModel().Result.DisplayData, message.RequestText);

        return res switch
        {
            -1 => await SendMessagesAsync(new AllianceResponseMessage { Response = 44 }),
            -2 => await SendMessagesAsync(new AllianceResponseMessage { Response = 51 }),
            -3 => await SendMessagesAsync(new AllianceResponseMessage { Response = 53 }),
            -4 => await SendMessagesAsync(new AllianceResponseMessage { Response = 52 }),
            -5 => await SendMessagesAsync(new AllianceResponseMessage { Response = 54 }),
            _ => await SendMessagesAsync(new AllianceResponseMessage { Response = 50 })
        };
    }

    private async Task<bool> RespondToAllianceJoinRequestMessageReceived(RespondToAllianceJoinRequestMessage message)
    {
        var alliance = GrainHelper.GetAllianceGrain(grainFactory, state.AllianceId);

        var streamEntryAsBytes = await alliance.GetStreamEntryById(message.StreamId);

        if (streamEntryAsBytes == null)
            return await SendMessagesAsync(new AllianceResponseMessage { Response = 93 });

        var streamEntry = MessagePackSerializer.Deserialize<StreamEntry>(streamEntryAsBytes);

        var res = await alliance.JoinRequestActionAsync(message.StreamId, message.Accepted,
            state.AccountId, state.AvatarName);

        return res switch
        {
            -1 => await SendMessagesAsync(new AllianceResponseMessage { Response = 93 }),
            -2 => await SendMessagesAsync(new AllianceResponseMessage { Response = 95 }),
            -3 or -4 => await SendMessagesAsync(new AllianceResponseMessage { Response = 93 }),
            -5 => await SendMessagesAsync(new AllianceResponseMessage { Response = 94 }),
            -6 or -7 => await SendMessagesAsync(new AllianceResponseMessage { Response = 92 }),
            _ => await CheckOtherResults(res, streamEntry.AuthorId)
        };

        async Task<bool> CheckOtherResults(int result, long authorId)
        {
            switch (res)
            {
                case > 0 and < 3 when res != 1:
                    return await SendMessagesAsync(new AllianceResponseMessage { Response = 91 });
                case > 0 and < 3:
                {
                    var data = await alliance.GetAllianceInfoAndRoleAsync(authorId);

                    var streamEntriesAsBytes = await alliance.GetStreamEntries();

                    var streamEntries = streamEntriesAsBytes
                        .Select(x => MessagePackSerializer.Deserialize<StreamEntry>(x))
                        .ToList();

                    var targetSession = GrainHelper.GetPlayerSession(grainFactory, authorId);

                    await targetSession.SendMessagesToPlayerAsync([
                        LaserContractSerializer.SerializeToStruct(new AllianceResponseMessage { Response = 40 }),
                        LaserContractSerializer.SerializeToStruct(new MyAllianceMessage
                        {
                            OnlineMembers = 1,
                            MyAllianceObject = new MyAllianceObject
                            {
                                MyRoleDataRef = (data.Item2 ?? 0) + 25_000_000,
                                AllianceHeaderEntry = new AllianceHeaderEntry
                                {
                                    AllianceId = data.Item1.Item1.AllianceId,
                                    AllianceName = data.Item1.Item1.Name,
                                    BadgeGlobalId = data.Item1.Item1.BadgeGlobalId,
                                    AllianceType = data.Item1.Item1.AllianceType,
                                    MembersCount = data.Item1.Item2,
                                    NowTrophies = data.Item1.Item3,
                                    RequiredTrophies = data.Item1.Item1.RequiredTrophies,
                                    PreferredLanguageGlobalId = data.Item1.Item1.LanguageGlobalId,
                                    Region = LogicDataTables.GetDataById(data.Item1.Item1.RegionGlobalId)?.Name ??
                                             "RU"
                                }
                            }
                        }),
                        LaserContractSerializer.SerializeToStruct(new AllianceStreamMessage
                        {
                            StreamEntries = streamEntries
                        })
                    ]);

                    return await SendMessagesAsync(new AllianceResponseMessage { Response = 90 });
                }
                case < -1000:
                    res += 1000;
                    break;
                default:
                    return false;
            }

            return res switch
            {
                -100 or -101 or -1 => await SendMessagesAsync(new AllianceResponseMessage { Response = 93 }),
                -2 or -3 => await SendMessagesAsync(new AllianceResponseMessage { Response = 43 }),
                -4 => await SendMessagesAsync(new AllianceResponseMessage { Response = 45 }),
                -5 => await SendMessagesAsync(new AllianceResponseMessage { Response = 42 }),
                -6 => await SendMessagesAsync(new AllianceResponseMessage { Response = 46 }),
                -7 => await SendMessagesAsync(new AllianceResponseMessage { Response = 44 }),
                _ => false
            };
        }
    }

    private async Task<bool> AskForJoinableAlliancesListMessageReceived(AskForJoinableAlliancesListMessage message)
    {
        try
        {
            var res = await OpenSearchWorkerHelper.Worker.GetRandomAlliancesAsync(10,
                state.NowTrophies, LogicDataTables.GetDataByName<LogicRegionData>(state.Region)?.GlobalId ?? 0);

            var allist = new JoinableAllianceListMessage
            {
                Alliances = res
                    .Where(x => x.MembersCount > 0)
                    .Select(x => new AllianceHeaderEntry
                    {
                        AllianceId = x.Id,
                        AllianceName = x.Name,
                        BadgeGlobalId = x.BadgeGlobalId,
                        AllianceType = x.AllianceType,
                        MembersCount = x.MembersCount,
                        NowTrophies = x.NowTrophies,
                        RequiredTrophies = x.RequiredTrophies,
                        PreferredLanguageGlobalId = x.LanguageGlobalId,
                        Region = LogicDataTables.GetDataById(x.RegionGlobalId)?.Name ?? "RU"
                    })
                    .ToArray()
            };

            return await SendMessagesAsync(allist);
        }
        catch (Exception e)
        {
            Logger.Warn(e, "AskForJoinableAlliancesListMessageReceived Error!");

            var allist = new AllianceListMessage
            {
                Alliances = []
            };

            return await SendMessagesAsync(allist);
        }
    }

    private async Task<bool> SearchAlliancesMessageReceived(SearchAlliancesMessage message)
    {
        if (message.Name.StartsWith('#'))
        {
            var id = new LogicLongToCodeConverterUtil().ToId(message.Name);
            if (id <= 0)
                return true;

            var targetAlliance = GrainHelper.GetAllianceGrain(grainFactory, id);

            var data = await targetAlliance.GetAllianceInfoAndRoleAsync(0);

            if (data.Item1.Item1.OwnerAccountId <= 0)
                return true;

            if (data.Item1.Item2 <= 0)
                return true;

            return await SendMessagesAsync(
                new AllianceListMessage
                {
                    Alliances =
                    [
                        new AllianceHeaderEntry
                        {
                            AllianceId = data.Item1.Item1.AllianceId,
                            AllianceName = data.Item1.Item1.Name,
                            BadgeGlobalId = data.Item1.Item1.BadgeGlobalId,
                            AllianceType = data.Item1.Item1.AllianceType,
                            MembersCount = data.Item1.Item2,
                            NowTrophies = data.Item1.Item3,
                            RequiredTrophies = data.Item1.Item1.RequiredTrophies,
                            PreferredLanguageGlobalId = data.Item1.Item1.LanguageGlobalId,
                            Region = LogicDataTables.GetDataById(data.Item1.Item1.RegionGlobalId)?.Name ?? "RU"
                        }
                    ]
                });
        }

        var res = await OpenSearchWorkerHelper.Worker.SearchAllianceByNameAsync(message.Name, 64);

        return await SendMessagesAsync(
            new AllianceListMessage
            {
                Alliances = res
                    .Where(x => x.MembersCount > 0)
                    .Select(x => new AllianceHeaderEntry
                    {
                        AllianceId = x.Id,
                        AllianceName = x.Name,
                        BadgeGlobalId = x.BadgeGlobalId,
                        AllianceType = x.AllianceType,
                        MembersCount = x.MembersCount,
                        NowTrophies = x.NowTrophies,
                        RequiredTrophies = x.RequiredTrophies,
                        PreferredLanguageGlobalId = x.LanguageGlobalId,
                        Region = LogicDataTables.GetDataById(x.RegionGlobalId)?.Name ?? "RU"
                    })
                    .ToArray()
            });
    }

    private async Task<bool> GetPlayerProfileMessageReceived(GetPlayerProfileMessage message)
    {
        var targetHome = message.AccountId != state.AccountId
            ? GrainHelper.GetHomeGrain(grainFactory, message.AccountId)
            : grain;

        var v1 = await targetHome.GetAllianceIdAndAvatarName();

        if (v1.Item2.Length == 0)
            return false;

        var v2 = await targetHome.GetProfileData();

        var playerProfileMessage = new PlayerProfileMessage();

        if (v1.Item1 > 0)
        {
            var alliance = GrainHelper.GetAllianceGrain(grainFactory, v1.Item1);
            var resal = await alliance.GetAllianceInfoAndRoleAsync(message.AccountId);

            playerProfileMessage.AllianceHeader = new AllianceHeaderEntry
            {
                AllianceId = resal.Item1.Item1.AllianceId,
                AllianceName = resal.Item1.Item1.Name,
                BadgeGlobalId = resal.Item1.Item1.BadgeGlobalId,
                AllianceType = resal.Item1.Item1.AllianceType,
                MembersCount = resal.Item1.Item2,
                NowTrophies = resal.Item1.Item3,
                RequiredTrophies = resal.Item1.Item1.RequiredTrophies,
                PreferredLanguageGlobalId = resal.Item1.Item1.LanguageGlobalId,
                Region = LogicDataTables.GetDataById(resal.Item1.Item1.RegionGlobalId)?.Name ?? "RU"
            };

            playerProfileMessage.RoleGlobalId = (resal.Item2 ?? 0) + 25_000_000;
        }

        playerProfileMessage.PlayerProfile = new PlayerProfile
        {
            AccountId = message.AccountId,
            HeroEntries = v2.Item1,
            StatEntries = v2.Item2,
            DisplayData = v2.Item3
        };

        return await SendMessagesAsync(playerProfileMessage);
    }

    private async Task CancelAllInvitesBecauseInTeamAsync()
    {
        foreach (var invite in TeamInvites)
            try
            {
                var t = GrainHelper.GetTeamGrain(grainFactory, invite.Key);

                _ = t.ChangeInviteStatusAsync(1, 1, state.AccountId);

                await SendMessagesAsync(new TeamInvitationMessage
                {
                    Type = 0,
                    TeamInvitation = new TeamInvitation
                    {
                        TeamId = invite.Key,
                        FriendEntry = new FriendEntry
                        {
                            AccountId = invite.Value.AccountId,
                            DisplayData = new PlayerDisplayData
                            {
                                AvatarName = invite.Value.DisplayData?.AvatarName ?? "inviter",
                                Experience = 0,
                                NameColor = GlobalId.CreateGlobalId(43, 0),
                                Thumbnail = GlobalId.CreateGlobalId(28, 0)
                            }
                        }
                    }
                });
            }
            catch
            {
                // ignored
            }

        TeamInvites.Clear();
    }

    private async Task<bool> TeamCreateMessageReceived(TeamCreateMessage message)
    {
        var basic = await grain.GetBasicTeamMemberData();

        if (basic == null)
            return await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 10 });

        if (TeamRequests.Count != 0)
            return await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 102 });

        var teamId = GenerateTeamId();

        var memberEntry = new TeamMemberEntry
        {
            IsOwner = true,
            AccountId = state.AccountId,

            CharacterGlobalId = basic.CharacterGlobalId,
            SkinGlobalId = basic.SkinGlobalId,

            HeroTrophies = basic.HeroTrophies,
            HeroMaxTrophies = basic.HeroMaxTrophies,
            HeroPowerLevel = basic.HeroPowerLevel,

            State = 3,
            DifficultyLevel = basic.DifficultyLevel,
            DisplayData = basic.DisplayData,
            StarPowerGlobalId = basic.StarPowerGlobalId
        };

        var team = GrainHelper.GetTeamGrain(grainFactory, teamId);

        var res = await team.CreateTeamAsync(memberEntry, message.EventSlot, message.RoomType == 1);

        switch (res)
        {
            case -1 or -2: return await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 });
            case -101 or -102 or -103 or -104:
            case -105: return await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 33 });
            case -106: return await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 32 });
            case -107: return await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 31 });
        }

        MetricsClient.ObserveRoomRegistration();

        state.TeamId = teamId;
        state.TeamEventSlot = message.EventSlot;

        if (state.AllianceId > 0)
            _ = await team.AddOrChangeMemberAllianceIdAsync(state.AccountId, state.AllianceId);

        await CancelAllInvitesBecauseInTeamAsync();
        return true;

        long GenerateTeamId()
        {
            var randomBytes = new byte[8];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            var randomLong = BitConverter.ToInt64(randomBytes, 0);
            randomLong = Math.Abs(randomLong);

            return randomLong % int.MaxValue;
        }
    }

    private async Task<bool> TeamToggleMemberSideMessageReceived(TeamToggleMemberSideMessage message)
    {
        if (state.TeamId <= 0)
            return false;

        var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

        var res = await team.ToggleMemberSideAsync(message.MembersId.ElementAtOrDefault(0),
            message.MembersId.ElementAtOrDefault(1), message.Side);

        return res switch
        {
            -1 or -2 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 }),
            -3 or -4 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 33 }),
            _ => true
        };
    }

    private async ValueTask<(int?, int?)> GetFriendData(long friendId)
    {
        try
        {
            var friendship = GrainHelper.GetFriendshipGrain(grainFactory, state.AccountId);
            return await friendship.GetFriendStateAndReason(friendId);
        }
        catch
        {
            return (null, null);
        }
    }

    private async Task<bool> TeamAllianceMemberInviteMessageReceived(TeamAllianceMemberInviteMessage message)
    {
        if (state.AllianceId <= 0)
            return false;

        if (state.TeamId <= 0)
            return false;

        var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

        var friendData = await GetFriendData(message.PlayerId);

        var friendEntry = new FriendEntry
        {
            AccountId = state.AccountId,

            Trophies = state.NowTrophies,

            FriendState = friendData.Item1 ?? 0,
            FriendReason = friendData.Item2 ?? 0,

            Alliance = new FriendAllianceSegment
                { AllianceId = state.AllianceId, AllianceName = $"alliance_{state.AllianceId}" },

            DisplayData = new PlayerDisplayData
            {
                AvatarName = state.AvatarName,
                Experience = state.Experience,
                Thumbnail = state.ThumbnailGlobalId,
                NameColor = state.NameColorGlobalId
            }
        };

        var res = await team.AddInviteAsync(friendEntry, message.PlayerId, message.TeamIndex);

        return res switch
        {
            -1 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 }),
            -2 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 25 }),
            -3 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 27 }),
            -4 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 29 }),
            -5 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 44 }),
            -6 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 46 }),
            -7 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 27 }),
            -8 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 }),
            -9 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 24 }),
            -10 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 25 }),
            -11 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 40 }),
            -12 or -15 or -16 or -20 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 }),
            -13 or -14 or -17 or -18 or -19 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 25 }),
            -21 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 27 }),
            _ => true
        };
    }

    private async Task<int> TeamChangeMemberSettingsMessageReceived(TeamChangeMemberSettingsMessage message)
    {
        if (state.TeamId <= 0)
            return -1;

        var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

        if (HomeMode == null)
            return -1;

        if (message.StarPowerId != 0)
        {
            var card = LogicDataTables.GetDataById<LogicCardData>(message.StarPowerId);
            if (card == null)
                return -1;

            var characterSp = LogicDataTables.GetDataByName(16, card.Target);
            if (characterSp == null)
                return -2;

            if (!HomeMode.IsHeroUnlocked(characterSp.GlobalId, out var heroSp))
                return -3;

            if (!heroSp.GetCard(message.StarPowerId, out _)) return -4;

            foreach (var key in heroSp.StarPowersContainer.Keys.ToList())
                heroSp.StarPowersContainer[key] = false;

            heroSp.StarPowersContainer[message.StarPowerId] = true;

            if (message.SkinId == 0)
                goto s;
        }

        var skinData = LogicDataTables.GetDataById<LogicSkinData>(message.SkinId);
        if (skinData == null)
            return -1;

        if (!state.UnlockedSkins.Contains(message.SkinId) && !skinData.Name.EndsWith("Default"))
            return -2;

        var character = LogicHomeMode.SkinIdToCharacterData.GetValueOrDefault(message.SkinId);
        if (character == null)
            return -3;

        if (!HomeMode.IsHeroUnlocked(character.GlobalId, out _))
            return -4;

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var s in state.SelectedSkins.ToList())
        {
            var sCharacter = LogicHomeMode.SkinIdToCharacterData.GetValueOrDefault(s);
            if (sCharacter == null) continue;

            if (character.GlobalId == sCharacter.GlobalId)
                state.SelectedSkins.Remove(s);
        }

        state.SelectedSkins.Add(message.SkinId);
        state.HomeBrawlerGlobalId = character.GlobalId;

        s:
        var r = HomeMode.IsHeroUnlocked(state.HomeBrawlerGlobalId, out var hero);

        if (!r)
            return -5;

        var data = new TeamMemberData
        {
            DisplayData = new PlayerDisplayData
            {
                AvatarName = state.AvatarName,
                Experience = state.Experience,
                Thumbnail = state.ThumbnailGlobalId,
                NameColor = state.NameColorGlobalId
            },

            CharacterGlobalId = hero!.CharacterGlobalId,
            SkinGlobalId = state.SelectedSkins.FirstOrDefault(x =>
                LogicHomeMode.SkinIdToCharacterData[x].GlobalId == hero.CharacterGlobalId),

            HeroTrophies = hero.Trophies,
            HeroMaxTrophies = hero.MaxTrophies,
            HeroPowerLevel = hero.PowerLevel,

            StarPowerGlobalId = hero.StarPowersContainer.FirstOrDefault(x => x.Value).Key,

            DifficultyLevel = state.Events.FirstOrDefault(x => x.TicketEventDifficulty > 0)?.TicketEventDifficulty ?? 0
        };

        var res = await team.ChangeMemberSettingsAsync(state.AccountId, data);

        switch (res)
        {
            case -1:
            {
                await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 });
                return -6;
            }
        }

        return 0;
    }

    private async Task<bool> TeamChatMessageReceived(TeamChatMessage message)
    {
        if (state.TeamId <= 0)
            return false;

        if (message.Message.Length is <= 0 or >= 256)
            return false;

        var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

        var res = await team.SendTextStreamEntryAsync(state.AccountId, state.AvatarName, message.Message);

        return res switch
        {
            -1 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 }),
            _ => true
        };
    }

    private async Task<bool> TeamClearInviteMessageReceived(TeamClearInviteMessage message)
    {
        if (state.TeamId <= 0)
            return false;

        var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

        var res = await team.ChangeInviteStatusAsync(1, 0, message.InviteId);

        return res switch
        {
            -1 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 }),
            _ => true
        };
    }

    private async Task<bool> TeamInvitationResponseMessageReceived(TeamInvitationResponseMessage message)
    {
        if (state.TeamId > 0)
        {
            await CancelAllInvitesBecauseInTeamAsync();
            return true;
        }

        var g = TeamInvites.TryRemove(message.TeamId, out var inviter);

        if (!g)
            return false;

        if (message.MutePlayer && inviter != null)
            MutedPlayers.AddOrUpdate(
                inviter.AccountId,
                DateTime.UtcNow + TimeSpan.FromMinutes(10),
                (_, _) => DateTime.UtcNow + TimeSpan.FromMinutes(10)
            );

        var team = GrainHelper.GetTeamGrain(grainFactory, message.TeamId);

        var res = await team.ChangeInviteStatusAsync(2, message.Response == 1 ? 1 : 2, state.AccountId,
            await grain.GetBasicTeamMemberData());

        return res switch
        {
            -1 or -5 or -6 or -7 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 }),
            -2 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 43 }),
            -3 or -4 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 2 }),
            _ => await HandleDefaultCaseAsync()
        };

        async Task<bool> HandleDefaultCaseAsync()
        {
            if (message.Response != 1) return true;

            state.TeamId = message.TeamId;

            if (state.AllianceId > 0)
                _ = await team.AddOrChangeMemberAllianceIdAsync(state.AccountId, state.AllianceId);

            return true;
        }
    }

    private async Task<bool> TeamInviteMessageReceived(TeamInviteMessage message)
    {
        if (state.TeamId <= 0)
            return false;

        var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

        var friendData = await GetFriendData(message.PlayerId);

        var friendEntry = new FriendEntry
        {
            AccountId = state.AccountId,

            Trophies = state.NowTrophies,

            FriendState = friendData.Item1 ?? 0,
            FriendReason = friendData.Item2 ?? 0,

            Alliance = null,

            DisplayData = new PlayerDisplayData
            {
                AvatarName = state.AvatarName,
                Experience = state.Experience,
                Thumbnail = state.ThumbnailGlobalId,
                NameColor = state.NameColorGlobalId
            }
        };

        var res = await team.AddInviteAsync(friendEntry, message.PlayerId, message.TeamIndex);

        return res switch
        {
            -1 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 }),
            -2 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 25 }),
            -3 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 27 }),
            -4 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 29 }),
            -5 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 44 }),
            -6 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 46 }),
            -7 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 27 }),
            -8 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 }),
            -9 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 24 }),
            -10 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 25 }),
            -11 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 40 }),
            -12 or -15 or -16 or -20 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 }),
            -13 or -14 or -17 or -18 or -19 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 25 }),
            -21 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 27 }),
            _ => true
        };
    }

    private async Task<bool> TeamKickMessageReceived(TeamKickMessage message)
    {
        if (state.TeamId <= 0)
            return false;

        var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

        var res = await team.KickMemberAsync(state.AccountId, message.MemberId);

        if (res != 0)
            return await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 });

        return true;
    }

    private async Task<bool> TeamLeaveMessageReceived(TeamLeaveMessage message)
    {
        if (state.TeamId <= 0)
            return false;

        var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

        var res = await team.LeaveAsync(state.AccountId);

        switch (res)
        {
            case 0:
            {
                state.TeamId = 0;
                break;
            }
            case -1:
            {
                await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 });

                state.TeamId = 0;
                break;
            }
        }

        return true;
    }

    private async Task<bool> TeamMemberStatusMessageReceived(TeamMemberStatusMessage message)
    {
        _teamStatus = message.Status;

        if (state.TeamId <= 0)
            return false;

        var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

        var res = await team.TrySetMemberStatusAsync(state.AccountId, message.Status);

        if (res)
            return true;

        state.TeamId = 0;
        return await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 });
    }

    private async Task<bool> TeamPremadeChatMessageReceived(TeamPremadeChatMessage message)
    {
        if (state.TeamId <= 0)
            return false;

        var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

        var res = await team.SendPremadeStreamEntryAsync(state.AccountId, state.AvatarName, message.MessageGlobalId,
            message.EventSlot, message.DataId, message.TargetPlayer?.PlayerId ?? 0);

        return res switch
        {
            -1 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 }),
            -2 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 61 }),
            -3 or -4 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 62 }),
            -5 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 60 }),
            _ => true
        };
    }

    private async Task<bool> TeamRequestJoinApproveMessageReceived(TeamRequestJoinApproveMessage message)
    {
        if (message.MutePlayer)
            MutedPlayers.AddOrUpdate(
                message.JoinerId,
                DateTime.UtcNow + TimeSpan.FromMinutes(10),
                (_, _) => DateTime.UtcNow + TimeSpan.FromMinutes(10)
            );

        if (state.TeamId <= 0)
            return false;

        var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

        var res = await team.ChangeRequestStatusAsync(message.JoinerId, message.State ? 2 : 3);

        return res switch
        {
            -1 or -5 or -6 or -7 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 }),
            -2 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 43 }),
            -3 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 29 }),
            -4 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 2 }),
            _ => true
        };
    }

    private Task<bool> TeamRequestJoinCancelMessageReceived(TeamRequestJoinCancelMessage message)
    {
        foreach (var t in TeamRequests.Select(r => GrainHelper.GetTeamGrain(grainFactory, r)))
            _ = t.ChangeRequestStatusAsync(state.AccountId, 1);

        TeamRequests.Clear();
        return Task.FromResult(true);
    }

    private async Task<bool> TeamRequestJoinMessageReceived(TeamRequestJoinMessage message)
    {
        if (state.TeamId > 0)
        {
            var myTeam = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

            if (!await myTeam.IAmSolo(state.AccountId))
                return false;

            var r = await TeamLeaveMessageReceived(new TeamLeaveMessage());

            if (!r)
                return false;
        }

        var friendData = await GetFriendData(message.PlayerId);

        var friendEntry = new FriendEntry
        {
            AccountId = state.AccountId,

            Trophies = state.NowTrophies,

            FriendState = friendData.Item1 ?? 0,
            FriendReason = friendData.Item2 ?? 0,

            Alliance = state.AllianceId > 0
                ? new FriendAllianceSegment
                    { AllianceId = state.AllianceId, AllianceName = $"alliance_{state.AllianceId}" }
                : null,

            DisplayData = new PlayerDisplayData
            {
                AvatarName = state.AvatarName,
                Experience = state.Experience,
                Thumbnail = state.ThumbnailGlobalId,
                NameColor = state.NameColorGlobalId
            }
        };

        var team = GrainHelper.GetTeamGrain(grainFactory, message.TeamId);

        var res = await team.AddRequestAsync(friendEntry, message.PlayerId);

        // ReSharper disable once InvertIf
        if (res == 0)
        {
            TeamRequests.Add(message.TeamId);
            TeamIdsWithMyTrash.Add(message.TeamId);
        }

        return res switch
        {
            -1 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 }),
            -2 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 2 }),
            -3 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 45 }),
            -4 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 40 }),
            -5 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 46 }),
            -6 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 }),
            -7 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 24 }),
            -8 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 44 }),
            -9 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 48 }),
            _ => true
        };
    }

    private async Task<bool> TeamSetEventMessageReceived(TeamSetEventMessage message)
    {
        if (state.TeamId <= 0)
            return false;

        if (!state.EventSlots.Any(x => x.Slot == message.EventSlot && x.Unlocked))
            return false;

        var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

        var res = await team.SetEventAsync(message.EventSlot, state.AccountId);

        var ret = res switch
        {
            -1 or -2 or -3 or -4 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 }),
            -5 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 33 }),
            -6 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 32 }),
            -7 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 31 }),
            _ => true
        };

        if (!ret) return false;

        state.TeamEventSlot = message.EventSlot;
        return true;
    }

    private async Task<bool> TeamSetLocationMessageReceived(TeamSetLocationMessage message)
    {
        if (state.TeamId <= 0)
            return false;

        var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

        var res = await team.SetLocationAsync(message.LocationGlobalId, state.AccountId);

        return res switch
        {
            -1 or -2 or -3 or -4 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 }),
            -5 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 33 }),
            -6 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 20 }),
            -7 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 31 }),
            _ => true
        };
    }

    private async Task<bool> TeamSetMemberReadyMessageReceived(TeamSetMemberReadyMessage message)
    {
        if (state.TeamId <= 0)
            return false;

        if (MatchmakeId != null)
            return false;

        if (_inTeamSearchPlayer)
            return false;

        if (_playerTeamSearchBucket != null)
            return false;

        if (await grain.GetMyBattleServiceGrainId() != null)
            return false;

        if (BattleIdSpectate != null)
            return false;

        var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

        var res = await team.SetMemberReadyAsync(state.AccountId, message.IsReady, state.Region);

        return res switch
        {
            -1 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 }),
            -2 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 17 }),
            -3 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 30 }),
            -4 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 18 }),
            0 or 1000 => true,
            _ => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 })
        };
    }

    private async Task<bool> TeamSpectateMessageReceived(TeamSpectateMessage message)
    {
        var team = GrainHelper.GetTeamGrain(grainFactory, message.TeamId);

        var basic = await grain.GetBasicTeamMemberData();

        if (basic == null)
            return await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 1 });

        if (TeamRequests.Count != 0)
            await TeamRequestJoinCancelMessageReceived(new TeamRequestJoinCancelMessage());

        var memberEntry = new TeamMemberEntry
        {
            IsOwner = false,
            AccountId = state.AccountId,

            CharacterGlobalId = basic.CharacterGlobalId,
            SkinGlobalId = basic.SkinGlobalId,

            HeroTrophies = basic.HeroTrophies,
            HeroMaxTrophies = basic.HeroMaxTrophies,
            HeroPowerLevel = basic.HeroPowerLevel,

            State = 3,
            DifficultyLevel = basic.DifficultyLevel,
            DisplayData = basic.DisplayData,
            StarPowerGlobalId = basic.StarPowerGlobalId
        };

        var res = await team.JoinByIdAsync(memberEntry);

        // ReSharper disable once InvertIf
        if (res == 0)
        {
            state.TeamId = message.TeamId;

            if (state.AllianceId > 0)
                _ = await team.AddOrChangeMemberAllianceIdAsync(state.AccountId, state.AllianceId);
        }

        return res switch
        {
            -1 or -4 or -5 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 5 }),
            -2 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 3 }),
            -3 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 7 }),
            -6 => await SendMessagesAsync(new TeamErrorMessage { ErrorCode = 2 }),
            _ => true
        };
    }

    private async Task<bool> GetLeaderboardMessageReceived(GetLeaderboardMessage message)
    {
        var lmessage = new LeaderboardMessage
        {
            LeaderboardType = message.LeaderboardType,
            BrawlerGlobalId = message.BrawlerGlobalId,
            Region = state.Region,
            IsRegional = message.IsRegional
        };

        var cache = message.IsRegional
            ? LeaderboardWorker.GetRegionCache(state.Region)
            : LeaderboardWorker.GetGlobalCache();

        switch (message.LeaderboardType)
        {
            case 1:
            {
                if (cache != null)
                {
                    lmessage.PlayerRankingDatas = cache.Item1.ToList();

                    var myRank = 0;
                    var players = cache.Item1;

                    for (var i = 0; i < players.Length; i++)
                    {
                        if (players[i].AccountId != state.AccountId) continue;

                        myRank = i + 1;
                        break;
                    }

                    lmessage.MyTrophies = state.NowTrophies;
                    lmessage.MyIndex = myRank;
                }
                else
                {
                    lmessage.PlayerRankingDatas = [];
                    lmessage.MyTrophies = state.NowTrophies;
                    lmessage.MyIndex = 0;
                }

                break;
            }
            case 2:
            {
                if (cache != null)
                {
                    lmessage.AllianceRankingDatas = cache.Item2.ToList();

                    var myRank = 0;
                    var alliances = cache.Item2;

                    for (var i = 0; i < alliances.Length; i++)
                    {
                        if (alliances[i].AllianceId != state.AllianceId) continue;

                        myRank = i + 1;
                        lmessage.MyTrophies = alliances[i].Trophies;

                        break;
                    }

                    lmessage.MyIndex = myRank;
                }
                else
                {
                    lmessage.AllianceRankingDatas = [];
                    lmessage.MyIndex = 0;
                }

                break;
            }
            default:
            {
                if (cache != null)
                {
                    lmessage.PlayerBrawlerRankingDatas = cache.Item3.AsValueEnumerable()
                        .Where(x => x.BrawlerGlobalId == message.BrawlerGlobalId)
                        .ToList();

                    var myRank = 0;
                    var players = lmessage.PlayerBrawlerRankingDatas;

                    for (var i = 0; i < players.Count; i++)
                    {
                        if (players[i].AccountId != state.AccountId) continue;

                        myRank = i + 1;
                        lmessage.MyTrophies = players[i].BrawlerTrophies;

                        break;
                    }

                    lmessage.MyIndex = myRank;
                }
                else
                {
                    lmessage.PlayerBrawlerRankingDatas = [];

                    lmessage.MyTrophies = state.HeroEntries
                        .FirstOrDefault(x => x.CharacterGlobalId == message.BrawlerGlobalId)?.Trophies ?? 0;

                    lmessage.MyIndex = 0;
                }

                break;
            }
        }

        return await SendMessagesAsync(lmessage);
    }

    private async Task<bool> LookForGameRoomRequestMessageReceived(LookForGameRoomRequestMessage message)
    {
        if (MatchmakeId != null)
            return false;

        var region = LogicDataTables.GetDataByName<LogicRegionData>(state.Region);

        if (region == null)
            return false;

        if (state.TeamId > 0)
        {
            var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

            var res = await team.StartPlayersSearchAsync(state.AccountId, region.GlobalId, state.NowTrophies);

            switch (res)
            {
                case -1 or -2 or -3 or -4 or -5 or -6:
                    return await SendMessagesAsync(new MatchMakingCancelledMessage(),
                        new TeamErrorMessage { ErrorCode = 1 });
                case 0:
                    _inTeamSearchPlayer = true;
                    return await SendMessagesAsync(new MatchMakingStatusMessage());
            }

            return false;
        }

        var @event = state.Events.FirstOrDefault(x => x.Id == message.EventId);

        if (@event == null)
            return false;

        _playerTeamSearchBucket = $"players_search:{@event.LocationGlobalId}";
        _playerTeamSearchTick = 1;

        return await SendMessagesAsync(new MatchMakingStatusMessage());
    }

    private async Task<bool> CancelMatchmakingMessageReceived(CancelMatchmakingMessage message)
    {
        if (state.TeamId > 0 && !_inTeamSearchPlayer)
        {
            var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

            var res = await team.CancelMatchmakingAsync(fromHomeMessageManager: true);

            return res == 0;
        }

        if (MatchmakeId != null && MatchmakingServiceGrain != null)
        {
            try
            {
                var r = await MatchmakingServiceGrain.RemovePlayersFromMatchmakingAsync(MatchmakeId.Value,
                    [state.AccountId]);

                if (r == -2)
                    return true;

                MatchmakingServiceGrain = null;
                MatchmakeId = null;
            }
            catch
            {
                MatchmakingServiceGrain = null;
                MatchmakeId = null;
            }

            return await SendMessagesAsync(new MatchMakingCancelledMessage());
        }

        if (_inTeamSearchPlayer)
        {
            var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);

            var res = await team.StopPlayersSearchAsync(state.AccountId);
            
            return res switch
            {
                -1 or -2 or -3 or -4 or -5 or -6 => await SendMessagesAsync(new MatchMakingCancelledMessage(),
                    new TeamErrorMessage { ErrorCode = 1 }),
                0 => true,
                _ => false
            };
        }

        if (_playerTeamSearchBucket != null)
        {
            _playerTeamSearchBucket = null;
            _playerTeamSearchTick = 0;

            return await SendMessagesAsync(new MatchMakingCancelledMessage());
        }

        return true;
    }

    private async Task<bool> MatchmakeRequestMessageReceived(MatchmakeRequestMessage message)
    {
        if (EventsManager.GetMaintenanceSecondsLeft() > 0)
            return await SendMessagesAsync(new MatchmakeFailedMessage { ErrorCode = 9 });

        if (await grain.GetMyBattleServiceGrainId() != null)
            return false;

        if (BattleIdSpectate != null)
            return false;

        if (MatchmakeId != null)
            return false;

        if (state.TeamId > 0)
            return false;

        if (_inTeamSearchPlayer)
            return false;

        if (_playerTeamSearchBucket != null)
            return false;

        if (HomeMode == null)
            return false;

        if (!HomeMode.IsHeroUnlocked(message.BrawlerGlobalId, out var h))
            return false;

        var sector = LogicGameModeUtil.GetMmSectorByTrophies(h.Trophies);

        var mm = GrainHelper.GetMatchmakingGrain(grainFactory, message.EventSlot, sector, state.Region.ToLower());

        var r = await mm.AddPlayersToMatchmakingAsync([(state.AccountId, state.TeamId)], h.Trophies,
            state.Region.ToLower());

        switch (r.Item1)
        {
            case -1 or -2 or -3 or -4 or -5:
                return await SendMessagesAsync(new OutOfSyncMessage());
        }

        MatchmakingServiceGrain = mm;
        MatchmakeId = r.Item2;

        return true;
    }

    private async Task<bool> ClientInfoMessageReceived(ClientInfoMessage message)
    {
        if (BattleServerIp == null)
            return false;

        return await SendMessagesAsync(new UdpConnectionInfoMessage
        {
            ServerIp = BattleServerIp,
            ServerPort = BattleServerPort,
            SessionLow = BattleServerSessionLow,
            SessionHigh = BattleServerSessionHigh,
            KaNaN = KaNaN
        });
    }

    private async Task<bool> StartSpectateMessageReceived(StartSpectateMessage message)
    {
        try
        {
            var target = GrainHelper.GetHomeGrain(grainFactory, message.AccountId);

            var battleId = await target.GetMyBattleServiceGrainId();

            if (battleId == null)
                return await SendMessagesAsync(new SpectateFailedMessage());

            var battle = GrainHelper.GetBattleGrain(grainFactory, battleId.Value);

            var res = await battle.AddSpectator(message.AccountId, state.AccountId, false);

            if (res != 0)
                return await SendMessagesAsync(new SpectateFailedMessage());

            var data = await battle.GetMySpectatorLoadingData(state.AccountId, B);

            if (data == null)
                return await SendMessagesAsync(new SpectateFailedMessage());

            var d = data.Value;

            BattleServerIp = d.ip;
            BattleServerPort = d.port;

            BattleServerSessionLow = d.lowSessionId;
            BattleServerSessionHigh = d.highSessionId;

            KaNaN = d.kanan;

            BattleIdSpectate = battleId.Value;

            if (d.startLoadingMessage != null)
                await grain.SendMessagesToPlayerAsync([d.startLoadingMessage.Value]);

            return await SendMessagesAsync(new UdpConnectionInfoMessage
            {
                ServerIp = BattleServerIp,
                ServerPort = BattleServerPort,
                SessionLow = BattleServerSessionLow,
                SessionHigh = BattleServerSessionHigh,
                KaNaN = KaNaN
            });
        }
        catch (Exception e)
        {
            Logger.Error(e);
            return await SendMessagesAsync(new SpectateFailedMessage());
        }
    }

    private async Task<bool> StopSpectateMessageReceived(StopSpectateMessage message)
    {
        try
        {
            if (BattleIdSpectate == null)
                return false;

            var battle = GrainHelper.GetBattleGrain(grainFactory, BattleIdSpectate.Value);

            _ = battle.RemoveSpectator(state.AccountId);

            BattleServerIp = null;
            BattleServerPort = 0;
            BattleServerSessionLow = 0;
            BattleServerSessionHigh = 0;
            BattleIdSpectate = null;

            await grain.SendOwnHomeDataAsync(false);
            return true;
        }
        catch (Exception e)
        {
            Logger.Error(e);
            return false;
        }
    }

    private async Task<bool> GetBattleLogMessageReceived(GetBattleLogMessage message)
    {
        await SendMessagesAsync(new BattleLogMessage
        {
            AllLogs = true,
            BattleLogEntries = []
        });

        return true;
    }

    public async Task StopPlayersSearchAsync()
    {
        _inTeamSearchPlayer = false;
        await SendMessagesAsync(new MatchMakingCancelledMessage());
    }

    private delegate Task<int> MessageProcessorDelegate(MessageManager manager, PiranhaMessage message);
}