using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using NLog;
using ZLinq;
using ZOVserver.Services.Game.HomeService.Laser.Mode;
using ZOVserver.Services.Game.HomeService.Leaderboard;
using ZOVserver.Services.Game.HomeService.States;
using ZOVserver.Shared.Abstractions;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Interfaces;
using ZOVserver.Shared.Contracts.Laser.Combined.Delivery;
using ZOVserver.Shared.Contracts.Laser.Combined.Entries;
using ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA;
using ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;
using ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Inheritors;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;
using ZOVserver.Shared.Contracts.Laser.Combined.Team;
using ZOVserver.Shared.Contracts.Laser.Commands;
using ZOVserver.Shared.Contracts.Laser.Commands.FromClient;
using ZOVserver.Shared.Contracts.Laser.Commands.ToClient;
using ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Game;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Services.Game.HomeService.Laser.Commands.Executors;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedParameter.Local")]
[SuppressMessage("Performance", "CA1822")]
internal class ClientCommandsExecutor(
    IHomeServiceGrain grain,
    HomeState state,
    LogicHomeMode homeMode,
    CommandManager commandManager,
    IMessageManager messageManager,
    IGrainFactory grainFactory)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly FrozenDictionary<int, CommandProcessorDelegate> Processors;

    static ClientCommandsExecutor()
    {
        var methods = typeof(ClientCommandsExecutor).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
        var cd = new Dictionary<int, CommandProcessorDelegate>();

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != 1) continue;

            var commandType = parameters[0].ParameterType;
            if (!commandType.IsSubclassOf(typeof(LogicCommand))) continue;

            var instance = (LogicCommand)Activator.CreateInstance(commandType)!;
            var commandId = instance.GetCommandType();

            cd.Add(commandId, CompileProcessor(method, commandType));
        }

        Processors = cd.ToFrozenDictionary();

        var commands = LogicCommandManager.GetAllSavedCommands();
        foreach (var command in commands)
            if (!Processors.ContainsKey(command) && command >= 500)
                throw new Exception($"Command {command} is not registered");
    }

    private static CommandProcessorDelegate CompileProcessor(MethodInfo method, Type commandType)
    {
        var executorParam = Expression.Parameter(typeof(ClientCommandsExecutor), "executor");
        var commandParam = Expression.Parameter(typeof(LogicCommand), "command");
        var methodCall = Expression.Call(executorParam, method, Expression.Convert(commandParam, commandType));

        Expression body = method.ReturnType switch
        {
            var t when t == typeof(Task<int>) => methodCall,
            var t when t == typeof(Task<bool>) => Expression.Call(
                typeof(ClientCommandsExecutor).GetMethod(nameof(ConvertTaskBoolToTaskInt),
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

    private async Task<int> ExecuteLogicSetPlayerNameColorCommand(LogicSetPlayerNameColorCommand command)
    {
        var nameColor = LogicDataTables.GetDataById<LogicNameColorData>(command.ColorGlobalId);

        if (nameColor == null)
            return -20001;

        state.NameColorGlobalId = command.ColorGlobalId;

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

    private async Task<int> ExecuteLogicSetPlayerThumbnailCommand(LogicSetPlayerThumbnailCommand command)
    {
        var playerThumbnail = LogicDataTables.GetDataById<LogicPlayerThumbnailData>(command.ThumbnailGlobalId);

        if (playerThumbnail == null)
            return -20001;

        if (playerThumbnail.RequiredHero != string.Empty)
        {
            var heroGlobalId = LogicDataTables.GetDataByName(16, playerThumbnail.RequiredHero);

            if (heroGlobalId == null)
                return -20011;

            if (!homeMode.IsHeroUnlocked(heroGlobalId.GlobalId, out _))
                return -20012;
        }

        if (playerThumbnail.RequiredTotalTrophies > state.MaxTrophies)
            return -20003;

        if (playerThumbnail.RequiredExpLevel > homeMode.GetPlayerLevel())
            return -20004;

        state.ThumbnailGlobalId = command.ThumbnailGlobalId;

        // ReSharper disable once InvertIf
        if (state.AllianceId > 0)
        {
            var alliance = GrainHelper.GetAllianceGrain(grainFactory, state.AllianceId);
            await alliance.ChangeMemberInfoAsync(state.AccountId, grain.GetAllianceDetailedHomeModel().Result);
        }

        await grain.UpdateMyFriendEntryInFriendshipAsync();

        return 0;
    }

    private int ExecuteLogicPurchaseHeroLvlUpMaterialCommand(LogicPurchaseHeroLvlUpMaterialCommand command)
    {
        if (command.PackIndex < 0 || command.PackIndex >= LogicConfData.GoldAmountInPacks.Length) return -20001;

        var cost = LogicConfData.GoldPacksCost[command.PackIndex];

        if (!homeMode.UseDiamonds(cost)) return -20002;

        state.Gold += LogicConfData.GoldAmountInPacks[command.PackIndex];

        if (string.IsNullOrWhiteSpace(state.SupportedContentCreator))
            return 0;

        var cg = grainFactory.GetGrain<IContentCreatorRewardServiceGrain>(state.SupportedContentCreator);
        _ = cg.AddSupportedDiamondsReward(cost);

        return 0;
    }

    private async Task<int> ExecuteLogicLevelUpCommand(LogicLevelUpCommand command)
    {
        if (!homeMode.IsHeroUnlocked(command.BrawlerGlobalId, out var hero))
            return -30001;

        if (!homeMode.UseUpgradeMaterials(LogicConfData.GoldToNextHeroLevel[hero.PowerLevel - 1])) return -30002;

        var p = 0;
        for (var i = 0; i < hero.PowerLevel; i++) p += LogicConfData.PowerPointsToNextHeroLevel[i];
        if (hero.PowerPoints < p) return -30003;

        hero.PowerLevel++;

        if (state.TeamId <= 0 || hero.CharacterGlobalId != state.HomeBrawlerGlobalId) return 0;

        var data = new TeamMemberData
        {
            DisplayData = new PlayerDisplayData
            {
                AvatarName = state.AvatarName,
                Experience = state.Experience,
                Thumbnail = state.ThumbnailGlobalId,
                NameColor = state.NameColorGlobalId
            },

            CharacterGlobalId = hero.CharacterGlobalId,
            SkinGlobalId = state.SelectedSkins.FirstOrDefault(x =>
                LogicHomeMode.SkinIdToCharacterData[x].GlobalId == hero.CharacterGlobalId),

            HeroTrophies = hero.Trophies,
            HeroMaxTrophies = hero.MaxTrophies,
            HeroPowerLevel = hero.PowerLevel,

            StarPowerGlobalId = hero.StarPowersContainer.FirstOrDefault(x => x.Value).Key,

            DifficultyLevel = state.Events.FirstOrDefault(x => x.TicketEventDifficulty > 0)?.TicketEventDifficulty ?? 0
        };

        await GrainHelper.GetTeamGrain(grainFactory, state.TeamId).SetMemberDataAsync(state.AccountId, data);
        return 0;
    }

    private int ExecuteLogicPurchaseDoubleCoinsCommand(LogicPurchaseDoubleCoinsCommand command)
    {
        if (!homeMode.UseDiamonds(LogicConfData.TokenDoublerCost)) return -30001;
        state.TokenDoublerCount += LogicConfData.TokenDoublerAmount;

        if (string.IsNullOrWhiteSpace(state.SupportedContentCreator))
            return 0;

        var cg = grainFactory.GetGrain<IContentCreatorRewardServiceGrain>(state.SupportedContentCreator);
        _ = cg.AddSupportedDiamondsReward(LogicConfData.TokenDoublerCost);

        return 0;
    }

    private int ExecuteLogicPurchaseTicketsCommand(LogicPurchaseTicketsCommand command)
    {
        if (command.PackIndex < 0 || command.PackIndex >= LogicConfData.TicketPacksCost.Length) return -30001;
        if (state.TicketPurchasedIndexes.Contains(command.PackIndex)) return -30002;

        var cost = LogicConfData.TicketPacksCost[command.PackIndex];

        if (!homeMode.UseDiamonds(cost)) return -30003;

        state.Tickets += LogicConfData.TicketsAmountInPacks[command.PackIndex];

        state.TicketPurchasedIndexes.Add(command.PackIndex);

        if (string.IsNullOrWhiteSpace(state.SupportedContentCreator))
            return 0;

        var cg = grainFactory.GetGrain<IContentCreatorRewardServiceGrain>(state.SupportedContentCreator);
        _ = cg.AddSupportedDiamondsReward(cost);

        return 0;
    }

    private int ExecuteLogicSelectStarPowerCommand(LogicSelectStarPowerCommand command)
    {
        var card = LogicDataTables.GetDataById<LogicCardData>(command.CardGlobalId);
        if (card == null)
            return -30001;

        var character = LogicDataTables.GetDataByName(16, card.Target);
        if (character == null)
            return -30002;

        if (!homeMode.IsHeroUnlocked(character.GlobalId, out var hero))
            return -30003;

        if (!hero.GetCard(command.CardGlobalId, out _)) return -30004;

        foreach (var key in hero.StarPowersContainer.Keys.ToList())
            hero.StarPowersContainer[key] = false;

        hero.StarPowersContainer[command.CardGlobalId] = true;
        return 0;
    }

    private int ExecuteLogicClearShopTickersCommand(LogicClearShopTickersCommand command)
    {
        foreach (var bundle in state.OfferBundles)
            bundle.State = 2;
        return 0;
    }

    private int ExecuteLogicHeroSeenCommand(LogicHeroSeenCommand command)
    {
        if (!homeMode.IsHeroUnlocked(command.HeroGlobalId, out var hero))
            return -30001;

        hero.CharacterState = command.State;
        return 0;
    }

    private async Task<int> ExecuteLogicSelectSkinCommand(LogicSelectSkinCommand command)
    {
        var skinData = LogicDataTables.GetDataById<LogicSkinData>(command.SkinGlobalId);
        if (skinData == null)
            return -30001;

        if (!state.UnlockedSkins.Contains(command.SkinGlobalId) && !skinData.Name.EndsWith("Default"))
            return -30002;

        var character = LogicHomeMode.SkinIdToCharacterData.GetValueOrDefault(command.SkinGlobalId);
        if (character == null)
            return -30003;

        if (!homeMode.IsHeroUnlocked(character.GlobalId, out _))
            return -30004;

        if (await grain.GetMyBattleServiceGrainId() != null)
            return -30005;

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var s in state.SelectedSkins.ToList())
        {
            var sCharacter = LogicHomeMode.SkinIdToCharacterData.GetValueOrDefault(s);
            if (sCharacter == null) continue;

            if (character.GlobalId == sCharacter.GlobalId)
                state.SelectedSkins.Remove(s);
        }

        state.SelectedSkins.Add(command.SkinGlobalId);
        state.HomeBrawlerGlobalId = character.GlobalId;

        return 0;
    }

    private async Task<int> ExecuteLogicSelectCharacterCommand(LogicSelectCharacterCommand command)
    {
        if (!homeMode.IsHeroUnlocked(command.BrawlerGlobalId, out var hero))
            return -30001;

        if (await grain.GetMyBattleServiceGrainId() != null)
            return -30002;

        state.HomeBrawlerGlobalId = hero.CharacterGlobalId;
        return 0;
    }

    private async Task<int> ExecuteLogicGatchaCommand(LogicGatchaCommand command)
    {
        if (state.TutorialState < 2) return -30000;

        switch (command.Index)
        {
            case < 1 or > 5:
                return -30001;
            case 2:
                return -30002;
        }

        switch (command.Index)
        {
            case 1 when !homeMode.UseDiamonds(30):
                return -30011;
            case 3 when !homeMode.UseDiamonds(80):
                return -30012;
            case 4 when !homeMode.UseBigBoxTokens(10):
                return -30013;
            case 5 when !homeMode.UseMiniBoxTokens(100):
                return -30014;
        }

        var boxType = command.Index switch
        {
            1 => BoxType.Big,
            3 => BoxType.Mega,
            4 => BoxType.Big,
            _ => BoxType.Mini
        };

        return await commandManager.SendCommandsAsync(
            new LogicGiveDeliveryItemsCommand
            {
                DeliveryUnits = homeMode.OpenBoxes(boxType, 1).ToList(),
                ForcedDrops = state.ForcedDrops
            });
    }

    private int ExecuteLogicUnlockFreeSkinsCommand(LogicUnlockFreeSkinsCommand command)
    {
        foreach (var hero in state.HeroEntries)
        {
            if (!LogicHomeMode.FreeSkinsForBrawler.TryGetValue(hero.CharacterGlobalId, out var skins)) continue;

            foreach (var skin in skins)
                if (!state.UnlockedSkins.Contains(skin.GlobalId))
                    state.UnlockedSkins.Add(skin.GlobalId);
        }

        return 0;
    }

    private int ExecuteLogicClaimDailyRewardCommand(LogicClaimDailyRewardCommand command)
    {
        if (state.TutorialState < 2) return -30000;

        // ReSharper disable once SimplifyLinqExpressionUseAll
        if (!state.EventSlots.Any(x => x.Slot == command.EventSlot && x.Unlocked))
            return -30001;

        var eventMn = state.Events.Find(x => x.Slot == command.EventSlot);
        if (eventMn == null)
            return -30002;

        if (eventMn.MiniBoxRewardClaimed)
            return -30003;

        state.MiniBoxTokens += eventMn.MiniBoxReward;
        eventMn.MiniBoxRewardClaimed = true;

        if (eventMn.Slot is not (2 or 5)) return 0;

        foreach (var e in state.Events)
            if (e.Slot is 2 or 5)
                e.MiniBoxRewardClaimed = true;

        return 0;
    }

    private async Task<int> ExecuteLogicClaimRankUpRewardCommand(LogicClaimRankUpRewardCommand command)
    {
        if (state.TutorialState < 2) return -30000;

        switch (command.Type)
        {
            case 6:
            {
                var milestone = LogicDataTables.GetDataByName<LogicMilestoneData>($"goal_6_{state.TrophyRoadProgress}");
                if (milestone == null) return -30001;

                if (milestone.Progress + milestone.ProgressStart > state.MaxTrophies) return -30002;

                state.TrophyRoadProgress++;

                var r = homeMode.GetDeliveryUnits(milestone, command.DataRefX);
                if (r == null) return -30003;

                return await commandManager.SendCommandsAsync(
                    new LogicGiveDeliveryItemsCommand
                    {
                        DeliveryUnits = r,
                        RewardTrackType = command.Type,
                        RewardForRankType = state.TrophyRoadProgress,
                        ForcedDrops = state.ForcedDrops
                    });
            }
        }

        return 0;
    }

    private async Task<int> ExecuteLogicPurchaseOfferCommand(LogicPurchaseOfferCommand command)
    {
        if (state.TutorialState < 2) return -30000;
        if (command.OfferIndex < 0 || command.OfferIndex >= state.OfferBundles.Count) return -30001;

        var offerBundle = state.OfferBundles[command.OfferIndex];
        if (offerBundle == null!) return -30002;

        if (offerBundle.Purchased) return 0; // -30003;
        if (DateTime.UtcNow > offerBundle.EndTime) return -30004;

        switch (offerBundle.ShopPriceType)
        {
            case (int)ShopPriceTypeHelperTable.ByGems:
                if (!homeMode.UseDiamonds(offerBundle.OfferPrice))
                    return -2;

                if (!string.IsNullOrWhiteSpace(state.SupportedContentCreator))
                {
                    var cg = grainFactory.GetGrain<IContentCreatorRewardServiceGrain>(state.SupportedContentCreator);
                    _ = cg.AddSupportedDiamondsReward(offerBundle.OfferOldPrice);
                }

                break;
            case (int)ShopPriceTypeHelperTable.ByGold:
                if (!homeMode.UseUpgradeMaterials(offerBundle.OfferPrice)) return -3;
                break;
            case (int)ShopPriceTypeHelperTable.ByStarPoints:
                if (!homeMode.UseStarPoints(offerBundle.OfferPrice)) return -4;
                break;
            case (int)ShopPriceTypeHelperTable.NotCanBought:
                return 0;
            case (int)ShopPriceTypeHelperTable.ForWatch:
                return 0;
            default:
                return -30005;
        }

        offerBundle.Purchased = true;

        var deliveryUnits = new List<DeliveryUnit>();

        foreach (var gemOffer in offerBundle.LogicGemOffers.ToArray())
        {
            var u = homeMode.GetDeliveryUnits(gemOffer, command.SelectedDataGlobalId);
            if (u == null) continue;

            deliveryUnits.AddRange(u);
        }

        return await commandManager.SendCommandsAsync(
            new LogicGiveDeliveryItemsCommand
            {
                DeliveryUnits = deliveryUnits,
                ForcedDrops = state.ForcedDrops
            });
    }

    private async Task<int> ExecuteLogicViewInboxNotificationCommand(LogicViewInboxNotificationCommand command)
    {
        if (state.TutorialState < 2) return -30000;

        if (!state.Notifications.TryGetValue(command.Index, out var notification))
            return -30001;

        if (notification.AlreadyRead)
            return -30002;

        notification.AlreadyRead = true;

        // ReSharper disable once InvertIf
        if (notification.GetNotificationType() is 81)
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var n in state.Notifications.Values)
                if (n.GetNotificationType() is 81)
                    n.AlreadyRead = true;

        // ReSharper disable once InvertIf
        if (notification.GetNotificationType() is 82)
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var n in state.Notifications.Values)
                if (n.GetNotificationType() is 82)
                    n.AlreadyRead = true;

        var deliveryUnits = new List<DeliveryUnit>();

        switch (notification.GetNotificationType())
        {
            case 79 when notification is StarPointsNotification star:
            {
                List<ScoreChange> scoreChanges = [];

                foreach (var score in star.ScoreEntries)
                {
                    if (!homeMode.IsHeroUnlocked(score.BrawlerGlobalId, out var h)) return -3_079_1;

                    h.Trophies = score.BrawlerTrophies - score.BrawlerTrophyLoss;
                    state.StarPoints += score.StarPointsGained;

                    scoreChanges.Add(new ScoreChange
                    {
                        BrawlerGlobalId = h.CharacterGlobalId,
                        BrawlerTrophies = h.Trophies
                    });
                }

                state.NowTrophies = state.HeroEntries.AsValueEnumerable().Sum(x => x.Trophies);

                await commandManager.SendCommandsAsync(new LogicDecreaseHeroScoreCommand
                {
                    AllTrophiesNow = state.NowTrophies,
                    ScoreChanges = scoreChanges
                });

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
                                state.Events.FirstOrDefault(x => x.TicketEventDifficulty > 0)?.TicketEventDifficulty ??
                                0
                        };

                        await GrainHelper.GetTeamGrain(grainFactory, state.TeamId)
                            .SetMemberDataAsync(state.AccountId, data);
                    }
                }

                // ReSharper disable once InvertIf
                if (state.AllianceId > 0)
                {
                    var alliance = GrainHelper.GetAllianceGrain(grainFactory, state.AllianceId);
                    await alliance.ChangeMemberInfoAsync(state.AccountId, grain.GetAllianceDetailedHomeModel().Result);
                }

                await grain.UpdateMyFriendEntryInFriendshipAsync();

                _ = LeaderboardContainer.LeaderboardService.UpdatePlayerFullDataAsync(state.AccountId, null,
                    state.Region,
                    state.HeroEntries.ToArray().ToDictionary(x => x.CharacterGlobalId, y => y.Trophies));

                break;
            }
            case 88 when notification is CoinDoublerRewardNotification coin:
            {
                var u = homeMode.GetDeliveryUnits(
                    new LogicGemOffer { Type = (int)ShopItemHelperTable.CoinDoubler, Count = coin.TokenDoublers }, 0);

                if (u == null) return -3_088_1;
                deliveryUnits.AddRange(u);
                break;
            }

            case 89 when notification is GemRewardNotification gem:
            {
                var u = homeMode.GetDeliveryUnits(
                    new LogicGemOffer { Type = (int)ShopItemHelperTable.Gems, Count = gem.Gems }, 0);

                if (u == null) return -3_089_1;
                deliveryUnits.AddRange(u);
                break;
            }

            case 90 when notification is ResourceRewardNotification resource:
            {
                List<DeliveryUnit>? u = [];

                switch (resource.ResourceGlobalId)
                {
                    case 5_000_008:
                        u = homeMode.GetDeliveryUnits(
                            new LogicGemOffer
                                { Type = (int)ShopItemHelperTable.UpgradeMaterial, Count = resource.ResourceAmount },
                            0);

                        if (u == null) return -3_090_1;
                        break;
                    case 5_000_010:
                        u = homeMode.GetDeliveryUnits(
                            new LogicGemOffer
                                { Type = (int)ShopItemHelperTable.StarPoints, Count = resource.ResourceAmount }, 0);

                        if (u == null) return -3_090_2;
                        break;
                }

                deliveryUnits.AddRange(u);
                break;
            }

            case 91 when notification is TicketRewardNotification ticket:
            {
                var u = homeMode.GetDeliveryUnits(
                    new LogicGemOffer { Type = (int)ShopItemHelperTable.Ticket, Count = ticket.Tickets }, 0);

                if (u == null) return -3_091_1;
                deliveryUnits.AddRange(u);
                break;
            }

            case 93 when notification is HeroRewardNotification hero:
            {
                var gem = new LogicGemOffer { Type = (int)ShopItemHelperTable.GuaranteedHero, Count = 1 };
                gem.SetItem(hero.HeroGlobalId);

                var u = homeMode.GetDeliveryUnits(gem, 0);

                if (u == null)
                {
                    var brawlerRarity =
                        LogicHomeMode.CharacterToUnlockCard.GetValueOrDefault(hero.HeroGlobalId)?.Rarity;

                    if (brawlerRarity == null)
                        return -3_093_1;

                    var compensation = brawlerRarity switch
                    {
                        "common" => 10,
                        "rare" => 30,
                        "super_rare" => 80,
                        "epic" => 170,
                        "mega_epic" => 350,
                        "legendary" => 700,
                        _ => throw new ArgumentOutOfRangeException(brawlerRarity)
                    };

                    u = homeMode.GetDeliveryUnits(
                        new LogicGemOffer { Type = (int)ShopItemHelperTable.Gems, Count = compensation }, 0);
                }

                u ??=
                    homeMode.GetDeliveryUnits(
                        new LogicGemOffer { Type = (int)ShopItemHelperTable.UpgradeMaterial, Count = 1000 }, 0)!;

                deliveryUnits.AddRange(u);
                break;
            }

            case 94 when notification is SkinRewardNotification skin:
            {
                var gem = new LogicGemOffer { Type = (int)ShopItemHelperTable.SkinAndHero, Count = 1 };
                gem.SetItem(skin.SkinGlobalId, true);

                var u = homeMode.GetDeliveryUnits(gem, 0);

                if (u == null)
                {
                    var skind = LogicDataTables.GetDataById<LogicSkinData>(skin.SkinGlobalId);

                    if (skind == null)
                        return -3_094_1;

                    var compensation = skind.CostGems;

                    if (compensation <= 0)
                        compensation = skind.CostLegendaryTrophies / 100;

                    u = homeMode.GetDeliveryUnits(
                        new LogicGemOffer { Type = (int)ShopItemHelperTable.Gems, Count = compensation }, 0);
                }

                u ??=
                    homeMode.GetDeliveryUnits(
                        new LogicGemOffer { Type = (int)ShopItemHelperTable.UpgradeMaterial, Count = 1000 }, 0)!;

                deliveryUnits.AddRange(u);
                break;
            }
        }

        return await commandManager.SendCommandsAsync(
            new LogicGiveDeliveryItemsCommand
            {
                DeliveryUnits = deliveryUnits,
                ForcedDrops = state.ForcedDrops
            });
    }

    private Task<int> ExecuteLogicDeleteNotificationCommand(LogicDeleteNotificationCommand command)
    {
        return !state.Notifications.Remove(command.Index, out _)
            ? Task.FromResult(-30001)
            : Task.FromResult(0);
    }

    private delegate Task<int> CommandProcessorDelegate(ClientCommandsExecutor executor, LogicCommand command);
}