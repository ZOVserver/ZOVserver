using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using NLog;
using ZOVserver.Services.Game.HomeService.Laser.Mode;
using ZOVserver.Services.Game.HomeService.Settings;
using ZOVserver.Services.Game.HomeService.States;
using ZOVserver.Shared.Abstractions;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Interfaces;
using ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;
using ZOVserver.Shared.Contracts.Laser.Combined.Team;
using ZOVserver.Shared.Contracts.Laser.Commands;
using ZOVserver.Shared.Contracts.Laser.Commands.ToClient;

namespace ZOVserver.Services.Game.HomeService.Laser.Commands.Executors;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedParameter.Local")]
[SuppressMessage("Performance", "CA1822")]
internal class ServerCommandsExecutor(
    IHomeServiceGrain grain,
    HomeState state,
    LogicHomeMode homeMode,
    CommandManager commandManager,
    IMessageManager messageManager,
    IGrainFactory grainFactory)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly FrozenDictionary<int, CommandProcessorDelegate> Processors;

    static ServerCommandsExecutor()
    {
        var methods = typeof(ServerCommandsExecutor).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
        var cd = new Dictionary<int, CommandProcessorDelegate>();

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != 1) continue;

            var commandType = parameters[0].ParameterType;
            if (!commandType.IsSubclassOf(typeof(LogicServerCommand))) continue;

            var instance = (LogicServerCommand)Activator.CreateInstance(commandType)!;
            cd.Add(instance.GetCommandType(), CompileProcessor(method, commandType));
        }

        Processors = cd.ToFrozenDictionary();

        var commands = LogicCommandManager.GetAllSavedCommands();
        foreach (var command in commands)
            if (!Processors.ContainsKey(command) && command < 500)
                throw new Exception($"ServerCommand {command} is not registered");
    }

    private static CommandProcessorDelegate CompileProcessor(MethodInfo method, Type commandType)
    {
        var executorParam = Expression.Parameter(typeof(ServerCommandsExecutor), "executor");
        var commandParam = Expression.Parameter(typeof(LogicCommand), "command");
        var methodCall = Expression.Call(executorParam, method, Expression.Convert(commandParam, commandType));

        Expression body = method.ReturnType switch
        {
            var t when t == typeof(Task<int>) => methodCall,
            var t when t == typeof(Task<bool>) => Expression.Call(
                typeof(ServerCommandsExecutor).GetMethod(nameof(ConvertTaskBoolToTaskInt),
                    BindingFlags.Static | BindingFlags.NonPublic)!, methodCall),
            var t when t == typeof(int) => Expression.Call(typeof(Task), nameof(Task.FromResult), [typeof(int)],
                methodCall),
            var t when t == typeof(bool) => Expression.Call(typeof(Task), nameof(Task.FromResult), [typeof(int)],
                Expression.Condition(methodCall, Expression.Constant(0), Expression.Constant(-10))),
            _ => Expression.Call(typeof(Task), nameof(Task.FromResult), [typeof(int)], Expression.Constant(-2))
        };

        return Expression.Lambda<CommandProcessorDelegate>(body, executorParam, commandParam).Compile();
    }

    private static async Task<int> ConvertTaskBoolToTaskInt(Task<bool> task)
    {
        return await task ? 0 : -10;
    }

    internal async Task<int> ExecuteCommandAsync(LogicCommand logicCommand)
    {
        if (state.GameState == 0) return -1001;
        if (!Processors.TryGetValue(logicCommand.GetCommandType(), out var processor)) return -1005;

        try
        {
            return await processor(this, logicCommand);
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
            return -1010;
        }
    }

    private async Task<int> ExecuteLogicChangeAvatarNameCommand(LogicChangeAvatarNameCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.NewName))
            return -30001;

        if (command.NewName.Length is < 3 or > 15)
            return -30002;

        if ((state.NextNameChangeTime - DateTime.UtcNow).TotalSeconds > 0)
            return -30003;

        if (homeMode.GetPlayerLevel() < 5 && state.NameSetByUser)
            return -30004;

        if (!homeMode.UseDiamonds(command.ChangeNamePrice))
            return -30005;

        state.AvatarName = command.NewName;
        state.NameSetByUser = true;

        state.NextNameChangePrice = Math.Min(Math.Max(++state.NumberOfNameChanges - 1, 0) * 30, 150);
        state.NextNameChangeTime = DateTime.UtcNow +
                                   TimeSpan.FromHours(HomeSettings.GetConfig().TimeSettings
                                       .NicknameChangeCooldownHours);

        // ReSharper disable once InvertIf
        if (state.TeamId > 0)
        {
            var r = homeMode.IsHeroUnlocked(state.HomeBrawlerGlobalId, out var hero);

            if (r)
            {
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

                    DifficultyLevel =
                        state.Events.FirstOrDefault(x => x.TicketEventDifficulty > 0)?.TicketEventDifficulty ?? 0
                };

                await GrainHelper.GetTeamGrain(grainFactory, state.TeamId).SetMemberDataAsync(state.AccountId, data);
            }
        }

        // ReSharper disable once InvertIf
        if (state.AllianceId > 0)
        {
            var alliance = GrainHelper.GetAllianceGrain(grainFactory, state.AllianceId);
            await alliance.ChangeMemberInfoAsync(state.AccountId, grain.GetAllianceDetailedHomeModel().Result);
        }

        await grain.UpdateMyFriendEntryInFriendshipAsync();

        return 0;
    }

    private bool ExecuteLogicGemNameChangeStateChangedCommand(LogicGemNameChangeStateChangedCommand command)
    {
        if (command.NextNameChangePrice == -1)
            command.NextNameChangePrice = state.NextNameChangePrice;

        if (command.NextNameChangeSeconds == -1)
            command.NextNameChangeSeconds = (int)(state.NextNameChangeTime - DateTime.UtcNow).TotalSeconds;

        return true;
    }

    private async Task<int> ExecuteLogicInviteBlockingChangedCommand(LogicInviteBlockingChangedCommand command)
    {
        state.BlockInvites = command.State;

        // ReSharper disable once InvertIf
        if (state.AllianceId > 0)
        {
            var alliance = GrainHelper.GetAllianceGrain(grainFactory, state.AllianceId);
            await alliance.ChangeMemberInfoAsync(state.AccountId, grain.GetAllianceDetailedHomeModel().Result);
        }

        await grain.UpdateMyFriendOnlineStatusEntryInFriendshipAsync();

        return 0;
    }

    private int ExecuteLogicGiveDeliveryItemsCommand(LogicGiveDeliveryItemsCommand command)
    {
        if (command.DeliveryUnits == null) return -30001;

        foreach (var gatchaDrop in command.DeliveryUnits.SelectMany(deliveryUnit => deliveryUnit.GatchaDrops))
            switch (gatchaDrop.Type)
            {
                case 7:
                    state.Gold += gatchaDrop.Count;
                    break;
                case 2:
                    state.TokenDoublerCount += gatchaDrop.Count;
                    break;
                case 3:
                    state.Tickets += gatchaDrop.Count;
                    break;
                case 8:
                    state.Diamonds += gatchaDrop.Count;
                    break;
                case 12:
                    state.StarPoints += gatchaDrop.Count;
                    break;
            }

        return 0;
    }

    private bool ExecuteLogicKeyPoolChangedCommand(LogicKeyPoolChangedCommand command)
    {
        return command.AvailableBattleTokens <= LogicConfData.MaxBattleTokens;
    }

    private bool ExecuteLogicOffersChangedCommand(LogicOffersChangedCommand command)
    {
        return true;
    }

    private bool ExecuteLogicAddNotificationCommand(LogicAddNotificationCommand command)
    {
        return true;
    }

    private bool ExecuteLogicDayChangedCommand(LogicDayChangedCommand command)
    {
        return command.LogicConfData != null;
    }

    private bool ExecuteLogicSetSupportedCreatorCommand(LogicSetSupportedCreatorCommand command)
    {
        if (command.Code == null)
            return false;

        state.SupportedContentCreator = command.Code.Code;
        return true;
    }

    private bool ExecuteLogicDecreaseHeroScoreCommand(LogicDecreaseHeroScoreCommand command)
    {
        return command.AllTrophiesNow >= 0;
    }

    private delegate Task<int> CommandProcessorDelegate(ServerCommandsExecutor executor, LogicCommand command);
}