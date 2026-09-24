using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using MessagePack;
using NLog;
using ZLinq;
using ZOVserver.Services.Game.HomeService.Laser.Commands;
using ZOVserver.Services.Game.HomeService.Manager;
using ZOVserver.Services.Game.HomeService.Settings;
using ZOVserver.Services.Game.HomeService.States;
using ZOVserver.Shared.Abstractions;
using ZOVserver.Shared.Contracts.Events.Micro;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Interfaces;
using ZOVserver.Shared.Contracts.Laser.Combined.Delivery;
using ZOVserver.Shared.Contracts.Laser.Combined.Entries;
using ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA;
using ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;
using ZOVserver.Shared.Contracts.Laser.Combined.Notifications;
using ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Inheritors;
using ZOVserver.Shared.Contracts.Laser.Commands;
using ZOVserver.Shared.Contracts.Laser.Commands.ToClient;
using ZOVserver.Shared.Contracts.Laser.Messages.Server;
using ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Extension;
using ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Game;
using ZOVserver.Shared.TitanRemnants.Mathem;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Services.Game.HomeService.Laser.Mode;

public class LogicHomeMode(
    IHomeServiceGrain grain,
    HomeState state,
    IMessageManager messageManager,
    CommandManager commandManager,
    IPlayerSessionServiceGrain playerSession,
    IGrainFactory grainFactory)
{
    private const bool GenerateSuperOffer = true;
    private const bool GenerateSkinsForGems = true;
    private const bool GenerateSkinsByStarPoints = true;
    private const bool GenerateDailyOffers = true;

    private static readonly int FullBrawlerPoints = LogicConfData.PowerPointsToNextHeroLevel.Sum();

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly LogicCardData[] StarPowers = LogicDataTables.GetAllDataByClassId<LogicCardData>(23)!
        .AsValueEnumerable()
        .Where(x => x.Name.Contains("_unique"))
        .Where(x => !x.LockedForChronos)
        .ToArray();

    private static readonly LogicCharacterData[] AllBrawlers =
        LogicDataTables.GetAllDataByClassId<LogicCharacterData>(16)!
            .AsValueEnumerable()
            .Where(x => x.IsHero())
            .Where(x => !x.Disabled)
            .Where(x => !x.LockedForChronos)
            .ToArray();

    public static readonly FrozenDictionary<int, LogicCharacterData> SkinIdToCharacterData =
        LogicDataTables.GetAllDataByClassId<LogicSkinData>(29)!
            .AsValueEnumerable()
            .Select(x => (x.GlobalId, LogicDataTables.GetDataByName<LogicSkinConfData>(x.Conf)!))
            .Select(x => (x.Item1, LogicDataTables.GetDataByName<LogicCharacterData>(x.Item2.Character)!))
            .ToDictionary(x => x.Item1, x => x.Item2)
            .ToFrozenDictionary();

    public static readonly FrozenDictionary<int, LogicSkinData[]> FreeSkinsForBrawler = SkinIdToCharacterData
        .AsValueEnumerable()
        .Select(x => (LogicDataTables.GetDataById<LogicSkinData>(x.Key)!, x.Value))
        .Where(x => x.Item1.ObtainType is 5 or 6)
        .GroupBy(x => x.Value.GlobalId)
        .ToDictionary(
            g => g.Key,
            g => g.Select(x => x.Item1).ToArray()
        )
        .ToFrozenDictionary();

    private static readonly LogicCardData[] BrawlersUnlockCard = LogicDataTables.GetAllDataByClassId<LogicCardData>(23)!
        .AsValueEnumerable()
        .Where(x => x.TypeInCsv == "unlock")
        .ToArray();

    public static readonly FrozenDictionary<int, LogicCardData> CharacterToUnlockCard = BrawlersUnlockCard
        .AsValueEnumerable()
        .Select(x => (LogicDataTables.GetDataByName<LogicCharacterData>(x.Target)!, x))
        .Where(x => x.Item1.IsHero() && !x.Item1.Disabled && !x.Item1.LockedForChronos)
        .ToDictionary(x => x.Item1.GlobalId, x => x.x)
        .ToFrozenDictionary();

    private static readonly LogicCharacterData[] CommonBrawlers = FilterBrawlersByRarity("common");
    private static readonly LogicCharacterData[] RareBrawlers = FilterBrawlersByRarity("rare");
    private static readonly LogicCharacterData[] SuperRareBrawlers = FilterBrawlersByRarity("super_rare");
    private static readonly LogicCharacterData[] EpicBrawlers = FilterBrawlersByRarity("epic");
    private static readonly LogicCharacterData[] MythicBrawlers = FilterBrawlersByRarity("mega_epic");
    private static readonly LogicCharacterData[] LegendaryBrawlers = FilterBrawlersByRarity("legendary");

    private static readonly LogicCharacterData[] AllDropoutBrawlers = FilterBrawlersByKickedRarity("common");

    private static readonly LogicCharacterData[] AllPreciousBrawlers =
        EpicBrawlers.Concat(MythicBrawlers).Concat(LegendaryBrawlers).ToArray();

    private bool _lastStatusOfBoxesClosed;

    private bool _lastStatusOfDoubleTokensEvent;
    private bool _lastStatusOfShopClosed;

    private static int GetRandomInt()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }

    private static LogicCharacterData[] FilterBrawlersByRarity(string rarity)
    {
        return BrawlersUnlockCard
            .AsValueEnumerable()
            .Where(x => x.Rarity == rarity)
            .Select(x => LogicDataTables.GetDataByName<LogicCharacterData>(x.Target, 16)!)
            .Where(x => x != null! && x.IsHero() && !x.Disabled && !x.LockedForChronos)
            .ToArray();
    }

    private static LogicCharacterData[] FilterBrawlersByKickedRarity(string kickedRarity)
    {
        return BrawlersUnlockCard
            .AsValueEnumerable()
            .Where(x => x.Rarity != kickedRarity)
            .Select(x => LogicDataTables.GetDataByName<LogicCharacterData>(x.Target, 16)!)
            .Where(x => x != null! && x.IsHero() && !x.Disabled && !x.LockedForChronos)
            .ToArray();
    }

    public async Task TickAsync()
    {
        await UpdateBattleTokensAsync();
        await UpdateOffersAsync();
        await UpdateCustomOffersAsync();
        await CreateHomeEvents(true);
        await UpdateTrophySeason();
    }

    private async ValueTask UpdateTrophySeason()
    {
        var s = EventsManager.GetCurrentSeason();

        if (s.SeasonEndTime == default)
            return;

        if (s.SeasonCounter != state.MyLastSeasonCounter || s.SeasonEndTime < DateTime.UtcNow)
        {
            List<ScoreEntry> scoreEntryList = [];

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var hero in state.HeroEntries.ToArray())
            {
                var resetData = GetResetData(hero.Trophies);

                if (resetData.oldTrophies == -1)
                    continue;

                scoreEntryList.Add(new ScoreEntry
                {
                    BrawlerGlobalId = hero.CharacterGlobalId,
                    BrawlerTrophies = resetData.oldTrophies,
                    BrawlerTrophyLoss = resetData.trophiesToSubtract,
                    StarPointsGained = resetData.starPoints
                });
            }

            if (scoreEntryList.Count > 0)
                await grain.AddNotifications([
                    MessagePackSerializer.Serialize<BaseNotification>(new StarPointsNotification
                    {
                        NotificationIndex = GetRandomInt(),
                        CreationTime = DateTime.UtcNow,
                        Message = "",
                        ScoreEntries = scoreEntryList
                    })
                ]);

            state.MyLastSeasonCounter = s.SeasonCounter;
        }
    }

    private static (int oldTrophies, int trophiesToSubtract, int starPoints) GetResetData(int trophies)
    {
        var trophiesStart = SeasonRewardsMessage.TrophiesStart;
        var trophiesEnd = SeasonRewardsMessage.TrophiesEnd;
        var trophiesInReset = SeasonRewardsMessage.TrophiesInReset;
        var starPointsSeasonRewardAmount = SeasonRewardsMessage.StarPointsSeasonRewardAmount;

        if (trophies < trophiesStart[0])
            return (-1, -1, -1);

        var index = Array.FindIndex(trophiesStart, ts =>
            trophies <= (Array.IndexOf(trophiesStart, ts) >= trophiesEnd.Length - 1 ||
                         trophiesEnd[Array.IndexOf(trophiesStart, ts)] == -1
                ? int.MaxValue
                : trophiesEnd[Array.IndexOf(trophiesStart, ts)])
            && trophies >= ts);

        if (index == -1)
            index = trophiesStart.Length - 1;

        return (trophies, trophies - trophiesInReset[index], starPointsSeasonRewardAmount[index]);
    }

    public async Task GoodbyeAsync()
    {
        await UpdateBattleTokensAsync(false);
    }

    public async ValueTask CreateHomeEvents(bool sendCommand)
    {
        var sc = false;

        var events = EventsManager.GetEvents().ToArray();

        foreach (var e in events)
        {
            if (state.Events.Any(x => x.Id == e.EventId))
                continue;

            state.Events.RemoveAll(x => x.Slot == e.SlotId);

            state.Events.Add(CreateEvent(e, false));
            sc = true;
        }

        foreach (var e in state.Events.ToArray())
        {
            if (e.EndTime > DateTime.UtcNow) continue;

            state.Events.Remove(e);
            sc = true;
        }

        if (sc)
            if (state is { TeamId: > 0, TeamEventSlot: > 0 })
            {
                var team = GrainHelper.GetTeamGrain(grainFactory, state.TeamId);
                _ = team.SetEventAsync(state.TeamEventSlot, state.AccountId);
            }

        if (EventsManager.GetDoubleTokensEvent() != _lastStatusOfDoubleTokensEvent)
        {
            _lastStatusOfDoubleTokensEvent = EventsManager.GetDoubleTokensEvent();
            sc = true;
        }

        if (EventsManager.GetShopClosed() != _lastStatusOfShopClosed)
        {
            _lastStatusOfShopClosed = EventsManager.GetShopClosed();
            sc = true;
        }

        if (EventsManager.GetBoxesClosed() != _lastStatusOfBoxesClosed)
        {
            _lastStatusOfBoxesClosed = EventsManager.GetBoxesClosed();
            sc = true;
        }

        if (EventsManager.GetThemeGlobalId() != state.LobbyTheme)
        {
            state.LobbyTheme = EventsManager.GetThemeGlobalId();
            sc = true;
        }

        if (sc && sendCommand)
            await commandManager.SendCommandsAsync(new LogicDayChangedCommand { LogicConfData = CreateConfData() });
    }

    public LogicDailyData CreateDailyData()
    {
        var dailyData = new LogicDailyData
        {
            NameChangePrice = state.NextNameChangePrice,
            SecondsToNextNameChange = Math.Max(0, (int)(state.NextNameChangeTime - DateTime.UtcNow).TotalSeconds),
            NowTrophies = state.NowTrophies,
            MaxTrophies = state.MaxTrophies,
            TokenDoublerCount = state.TokenDoublerCount,
            SecondsToSeasonEnd =
                (int)Math.Abs((EventsManager.GetCurrentSeason().SeasonEndTime - DateTime.UtcNow).TotalSeconds),
            TrophyRoadProgress = state.TrophyRoadProgress,
            Experience = state.Experience,
            ThumbnailGlobalId = state.ThumbnailGlobalId,
            NameColorGlobalId = state.NameColorGlobalId,
            UnlockedSkins = state.UnlockedSkins,
            SelectedSkins = state.SelectedSkins,
            TokenLimitReached = state.TokenLimitReached,
            OfferBundles = state.OfferBundles,
            AvailableBattleTokens = state.AvailableBattleTokens,
            SecondsToNextBattleTokens = GetSecondsToNextBattleTokens(),
            Tickets = state.Tickets,
            HomeBrawlerGlobalId = state.HomeBrawlerGlobalId,
            StarBrawlers = state.StarBrawlers,
            Region = state.Region,
            SupportedContentCreator = state.SupportedContentCreator,
            TicketPurchasedIndexes = state.TicketPurchasedIndexes,
            ForcedDrops = state.ForcedDrops
        };

        if (state.BlockInvites)
            dailyData.IntValues.Add(new IntValueEntry { Key = 7, Value = 1 });

        if (state.MiniBoxOhdTokens > 0)
            dailyData.IntValues.Add(new IntValueEntry { Key = 3, Value = state.MiniBoxOhdTokens });

        if (state.BigBoxOhdTokens > 0)
            dailyData.IntValues.Add(new IntValueEntry { Key = 5, Value = state.BigBoxOhdTokens });

        if (state.TrophiesOhd > 0)
            dailyData.IntValues.Add(new IntValueEntry { Key = 4, Value = state.TrophiesOhd });

        if (state.LegendaryTrophiesOhd > 0)
            dailyData.IntValues.Add(new IntValueEntry { Key = 8, Value = state.LegendaryTrophiesOhd });

        state.MiniBoxOhdTokens = 0;
        state.BigBoxOhdTokens = 0;
        state.TrophiesOhd = 0;
        state.LegendaryTrophiesOhd = 0;

        return dailyData;
    }

    public LogicConfData CreateConfData()
    {
        return new LogicConfData
        {
            Events = state.Events,
            UpcomingEvents = GetUpcomingEvents(),

            IntValues =
            [
                new IntValueEntry
                    { Key = 1, Value = state.LobbyTheme },

                new IntValueEntry { Key = 14, Value = EventsManager.GetDoubleTokensEvent() ? 1 : 0 },
                new IntValueEntry { Key = 5, Value = EventsManager.GetShopClosed() ? 1 : 0 },
                new IntValueEntry { Key = 6, Value = EventsManager.GetBoxesClosed() ? 1 : 0 }
            ],

            ForceTicketsEventEnable = state.ForceTicketsEventEnable,

            ReleaseEntries =
            [
                /*new ReleaseEntry { BrawlerGlobalId = 16000029 },
                new ReleaseEntry { BrawlerGlobalId = 16000032 },
                new ReleaseEntry { BrawlerGlobalId = 23000192 },
                new ReleaseEntry { BrawlerGlobalId = 23000193 },
                new ReleaseEntry { BrawlerGlobalId = 23000210 },
                new ReleaseEntry { BrawlerGlobalId = 23000211 }*/
            ],

            EventSlots = state.EventSlots
        };
    }

    public static List<EventData> GetUpcomingEvents()
    {
        return EventsManager.GetUpcomingEvents().Select(x => CreateEvent(x, true)).ToList();
    }

    private static EventData CreateEvent(GameEventSlotEvent eventSlotEvent, bool upcoming)
    {
        return new EventData
        {
            Slot = eventSlotEvent.SlotId,
            Id = eventSlotEvent.EventId,
            LocationGlobalId = eventSlotEvent.LocationGlobalId,
            EndTime = eventSlotEvent.StartOrEndTime + TimeSpan.FromMilliseconds(1850),
            Modifiers = eventSlotEvent.Modifiers,
            MiniBoxReward = eventSlotEvent.MiniBoxTokensReward,
            IsUpcoming = upcoming
        };
    }

    private async ValueTask UpdateBattleTokensAsync(bool sendCommand = true)
    {
        var period = TimeSpan.FromMinutes(LogicConfData.BattleTokensRegenIntervalMinutes);

        if (state.AvailableBattleTokens >= LogicConfData.MaxBattleTokens)
            return;

        if (state.NextBattleTokensTime == default)
        {
            state.NextBattleTokensTime = DateTime.UtcNow + period;
            goto sendc;
        }

        var now = DateTime.UtcNow;

        if (now < state.NextBattleTokensTime)
            return;

        var passedPeriods = (int)((now - state.NextBattleTokensTime).TotalMinutes / period.TotalMinutes) + 1;

        state.AvailableBattleTokens += passedPeriods * LogicConfData.PlusBattleTokens;
        state.AvailableBattleTokens = Math.Min(state.AvailableBattleTokens, LogicConfData.MaxBattleTokens);

        state.NextBattleTokensTime = DateTime.UtcNow + period;

        if (state.AvailableBattleTokens >= LogicConfData.MaxBattleTokens)
            state.NextBattleTokensTime = default;

        sendc:
        if (sendCommand)
            await commandManager.SendCommandsAsync(new LogicKeyPoolChangedCommand
            {
                AvailableBattleTokens = state.AvailableBattleTokens,
                SecondsToNextBattleTokens = GetSecondsToNextBattleTokens()
            });
    }

    public int GetSecondsToNextBattleTokens()
    {
        if (state.AvailableBattleTokens >= LogicConfData.MaxBattleTokens)
            return -1;

        var now = DateTime.UtcNow;

        if (now >= state.NextBattleTokensTime)
            return 0;

        return (int)(state.NextBattleTokensTime - now).TotalSeconds + 1;
    }

    private static DateTime GetLastMoscow11Am() // ZoV
    {
        var moscowNow = DateTime.UtcNow.AddHours(3);

        var candidate = new DateTime(moscowNow.Year, moscowNow.Month, moscowNow.Day, 11, 0, 0);

        if (moscowNow.TimeOfDay < new TimeSpan(11, 0, 0))
            candidate = candidate.AddDays(-1);

        return candidate.AddHours(-3);
    }

    private async Task UpdateOffersAsync()
    {
        var now = DateTime.UtcNow;
        var lmt = GetLastMoscow11Am();
        var heroes = state.HeroEntries.ToArray();

        var removed = 0;

        foreach (var offer in state.OfferBundles.ToArray())
        {
            if (now >= offer.EndTime)
            {
                state.OfferBundles.Remove(offer);

                if (offer.CustomId != Guid.Empty)
                    state.RemovedCustomOffers.Add(offer.CustomId);

                removed++;
                continue;
            }

            foreach (var g in offer.LogicGemOffers.ToArray())
            {
                var a = false;

                switch (g.Type)
                {
                    case 3:
                    {
                        if (IsHeroUnlocked(g.ItemGlobalId, out _))
                            a = true;

                        break;
                    }
                    case 4:
                    {
                        if (state.UnlockedSkins.Contains(g.ItemGlobalId))
                            a = true;

                        break;
                    }
                }

                if (!a)
                    continue;

                offer.LogicGemOffers.Remove(g);
                removed++;
            }

            if (offer.LogicGemOffers.Count != 0)
                continue;

            state.OfferBundles.Remove(offer);

            if (offer.CustomId != Guid.Empty)
                state.RemovedCustomOffers.Add(offer.CustomId);
        }

        if (DateTime.UtcNow - state.LastUpdateOffersTime < TimeSpan.FromDays(1))
        {
            if (removed > 0)
                await commandManager.SendCommandsAsync(new LogicOffersChangedCommand
                    { OfferBundles = state.OfferBundles });
            return;
        }

        state.TicketPurchasedIndexes.Clear();

        if (GenerateSuperOffer) // super offer.
            if (Random.Shared.Next(0, 100) < (state.LastUpdateOffersTime == default ? 95 : 25))
            {
                var ownedBrawlerIds = new HashSet<int>(heroes.Select(h => h.CharacterGlobalId));

                var availableBrawlers = AllPreciousBrawlers
                    .AsValueEnumerable()
                    .Where(x => !ownedBrawlerIds.Contains(x.GlobalId))
                    .ToArray();

                if (availableBrawlers.Length == 0)
                    goto l2;

                var randomBrawlerIndex = Random.Shared.Next(availableBrawlers.Length);
                var randomBrawler = availableBrawlers[randomBrawlerIndex];

                var randomBrawlerRarity =
                    CharacterToUnlockCard.GetValueOrDefault(randomBrawler.GlobalId)?.Rarity;
                if (randomBrawlerRarity == null)
                    goto l2;

                var costInGems = randomBrawlerRarity switch
                {
                    "epic" => 170,
                    "mega_epic" => 350,
                    "legendary" => 700,
                    _ => throw new ArgumentOutOfRangeException()
                };

                var g1 = new LogicGemOffer { Type = 3, Count = 1 };
                g1.SetItem(randomBrawler.GlobalId);

                var microChance = Random.Shared.Next(0, 250) < 1;

                state.OfferBundles.Add(new LogicOfferBundles
                {
                    LogicGemOffers = [g1],
                    OfferHeader = new ChronosTextEntry { Type = 1, Text = "TID_IAP_VERY_SPECIAL_OFFER" },
                    BackgroundTheme = "offer_special",
                    OfferPrice = microChance ? costInGems / 10 : costInGems / 2 - 1,
                    OfferOldPrice = costInGems,
                    EndTime = now + TimeSpan.FromDays(2)
                });
            }

        l2:
        if (GenerateSkinsForGems) // gems skin offers;
        {
            var skinsA = new List<int>();

            var maxSkinsToAdd = heroes.Length switch
            {
                <= 4 => heroes.Length,
                _ => 5
            };

            var skins = LogicDataTables.GetAllDataByClassId<LogicSkinData>(29)!
                .AsValueEnumerable()
                .Where(s => !s.Name.Contains("Default") && s.CostGems > 1)
                .OrderBy(_ => Random.Shared.Next())
                .ToArray();

            var heroSkins = heroes
                .AsValueEnumerable()
                .Select(heroEntry =>
                    LogicDataTables.GetDataById<LogicCharacterData>(heroEntry.CharacterGlobalId)!.Name)
                .ToDictionary(x => x, _ => new List<int>());

            foreach (var skin in skins)
            {
                if (skinsA.Count >= maxSkinsToAdd) break;

                var conf = LogicDataTables.GetDataByName<LogicSkinConfData>(skin.Conf);
                if (conf == null) continue;

                var heroName = conf.Character;
                var skinGid = skin.GlobalId;

                if (!heroSkins.TryGetValue(heroName, out var value)) continue;
                if (value.Count > 0) continue;

                if (state.UnlockedSkins.Contains(skinGid)) continue;
                if (skinsA.Contains(skinGid)) continue;

                value.Add(skinGid);
                skinsA.Add(skinGid);

                var g1 = new LogicGemOffer { Type = 4, Count = 1 };
                g1.SetItem(skinGid, true);

                var origCost = skin.CostGems;
                var offerCost = Random.Shared.Next(0, 100) < 15 && origCost > 30 ? origCost / 2 - 1 : origCost;

                state.OfferBundles.Add(new LogicOfferBundles
                {
                    LogicGemOffers = [g1],
                    OfferHeader = new ChronosTextEntry { Type = 1, Text = "TID_SHOP_SPECIAL_OFFER" },
                    BackgroundTheme = "offer_special",
                    OfferPrice = offerCost,
                    OfferOldPrice = origCost,
                    EndTime = lmt + TimeSpan.FromHours(24)
                });
            }
        }

        if (GenerateSkinsByStarPoints) // star points skin offers;
        {
            var skinsA = new List<int>();

            var skins = LogicDataTables.GetAllDataByClassId<LogicSkinData>(29)!
                .AsValueEnumerable()
                .Where(s => !s.Name.Contains("Default") && s.CostLegendaryTrophies > 1)
                .OrderBy(_ => Random.Shared.Next())
                .ToArray();

            var heroSkins = heroes
                .AsValueEnumerable()
                .Select(heroEntry =>
                    LogicDataTables.GetDataById<LogicCharacterData>(heroEntry.CharacterGlobalId)!.Name)
                .ToDictionary(x => x, _ => new List<int>());

            foreach (var skin in skins)
            {
                if (skinsA.Count >= 2) break;

                var conf = LogicDataTables.GetDataByName<LogicSkinConfData>(skin.Conf);
                if (conf == null) continue;

                var heroName = conf.Character;
                var skinGid = skin.GlobalId;

                if (!heroSkins.TryGetValue(heroName, out var value)) continue;
                if (value.Count > 0) continue;

                if (state.UnlockedSkins.Contains(skinGid)) continue;
                if (skinsA.Contains(skinGid)) continue;

                value.Add(skinGid);
                skinsA.Add(skinGid);

                var g1 = new LogicGemOffer { Type = 4, Count = 1 };
                g1.SetItem(skinGid, true);

                state.OfferBundles.Add(new LogicOfferBundles
                {
                    LogicGemOffers = [g1],
                    OfferHeader = new ChronosTextEntry { Type = 1, Text = "TID_SKIN_INFO_LEGENDARY" },
                    BackgroundTheme = "offer_special",
                    OfferPrice = skin.CostLegendaryTrophies,
                    ShopPriceType = 3,
                    OfferOldPrice = skin.CostLegendaryTrophies,
                    EndTime = lmt + TimeSpan.FromHours(24)
                });
            }
        }

        if (GenerateDailyOffers)
        {
            state.OfferBundles.Add(new LogicOfferBundles
            {
                LogicGemOffers = [new LogicGemOffer { Type = 6, Count = 1 }],
                OfferHeader = new ChronosTextEntry { Type = 0, Text = "" },
                BackgroundTheme = string.Empty,
                OfferPrice = 0,
                OfferOldPrice = 0,
                EndTime = lmt + TimeSpan.FromHours(24),
                IsDaily = true
            });

            var heroesWithoutFullPowerPoints =
                heroes
                    .AsValueEnumerable()
                    .Where(x => x.PowerPoints < FullBrawlerPoints)
                    .OrderBy(_ => Random.Shared.Next())
                    .ToArray();

            var ltf = heroes.Length < 5;

            var powerPoints = 0;
            var maxPowerPoints = ltf ? heroes.Length : 5;

            if (Random.Shared.Next(0, 100) < 20)
            {
                state.OfferBundles.Add(new LogicOfferBundles
                {
                    LogicGemOffers = [new LogicGemOffer { Type = 10, Count = 1 }],
                    OfferHeader = new ChronosTextEntry { Type = 0, Text = "" },
                    BackgroundTheme = string.Empty,
                    OfferPrice = 60,
                    OfferOldPrice = 80,
                    EndTime = lmt + TimeSpan.FromHours(24),
                    IsDaily = true
                });

                if (!ltf) maxPowerPoints--;
            }

            foreach (var hero in heroesWithoutFullPowerPoints)
            {
                var powerPointsAmount = Random.Shared.Next(5, 150);
                var count = Math.Min(powerPointsAmount, FullBrawlerPoints - hero.PowerPoints);

                var g1 = new LogicGemOffer { Type = 8, Count = count };
                g1.SetItem(hero.CharacterGlobalId);

                state.OfferBundles.Add(new LogicOfferBundles
                {
                    LogicGemOffers = [g1],
                    OfferHeader = new ChronosTextEntry { Type = 0, Text = "" },
                    BackgroundTheme = string.Empty,
                    OfferPrice = count * 2,
                    ShopPriceType = 1,
                    OfferOldPrice = count * 2,
                    EndTime = lmt + TimeSpan.FromHours(24),
                    IsDaily = true
                });

                if (++powerPoints == maxPowerPoints) break;
            }
        }

        await commandManager.SendCommandsAsync(new LogicOffersChangedCommand { OfferBundles = state.OfferBundles });
        state.LastUpdateOffersTime = lmt;
    }

    private async ValueTask UpdateCustomOffersAsync()
    {
        var n = false;

        var ota = OffersManager.GetOffersToAdd();
        var otr = OffersManager.GetOffersToRemove();

        foreach (var offer in state.OfferBundles.ToArray())
        {
            if (offer.CustomId == Guid.Empty) continue;

            if (ota.ContainsKey(offer.CustomId) && !otr.ContainsKey(offer.CustomId)) continue;

            state.OfferBundles.Remove(offer);
            state.RemovedCustomOffers.Add(offer.CustomId);

            n = true;
        }

        state.RemovedCustomOffers.RemoveWhere(id => !ota.ContainsKey(id));

        foreach (var (key, om) in ota)
        {
            var stime = om.StartTime;
            var etime = om.EndTime;

            if (stime > DateTime.UtcNow) continue;
            if (etime <= DateTime.UtcNow) continue;

            if (!om.ShowToNewUsers)
                if (state.HomeCreatedTime > stime)
                    continue;

            if (state.OfferBundles.Any(x => x.CustomId == key)) continue;
            if (state.RemovedCustomOffers.Contains(key)) continue;
            if (otr.ContainsKey(key)) continue;

            List<LogicGemOffer> gemOffers = [];
            foreach (var item in om.Items)
            {
                var gemOffer = new LogicGemOffer
                {
                    Type = item.ItemType
                };

                if (!string.IsNullOrWhiteSpace(item.BrawlerName))
                {
                    var b = LogicDataTables.GetDataByName(16, item.BrawlerName);
                    if (b == null) continue;

                    gemOffer.Count = 1;
                    gemOffer.SetItem(b.GlobalId);
                }
                else if (!string.IsNullOrWhiteSpace(item.SkinName))
                {
                    var s = LogicDataTables.GetDataByName(29, item.SkinName);
                    if (s == null) continue;

                    gemOffer.Count = 1;
                    gemOffer.SetItem(s.GlobalId, true);
                }
                else
                {
                    gemOffer.Count = item.Count;
                }

                gemOffers.Add(gemOffer);
            }

            state.OfferBundles.Add(
                new LogicOfferBundles
                {
                    OfferHeader = new ChronosTextEntry { Type = om.Name.Contains("TID_") ? 1 : 0, Text = om.Name },
                    ShopPriceType = om.PriceType,
                    OfferPrice = om.Price,
                    OfferOldPrice = om.OldPrice,
                    BackgroundTheme = om.Background,
                    CustomId = key,
                    IsDaily = om.IsDaily,
                    EndTime = etime,
                    LogicGemOffers = gemOffers
                });

            n = true;
        }

        if (n)
            await commandManager.SendCommandsAsync(new LogicOffersChangedCommand { OfferBundles = state.OfferBundles });
    }

    public bool UseDiamonds(int count)
    {
        if (state.Diamonds < count) return false;
        state.Diamonds -= count;
        return true;
    }

    public bool UseUpgradeMaterials(int count)
    {
        if (state.Gold < count) return false;
        state.Gold -= count;
        return true;
    }

    public bool UseStarPoints(int count)
    {
        if (state.StarPoints < count) return false;
        state.StarPoints -= count;
        return true;
    }

    public bool UseTickets(int count)
    {
        if (state.Tickets < count) return false;
        state.Tickets -= count;
        return true;
    }

    public bool UseBigBoxTokens(int count)
    {
        if (state.BigBoxStarTokens < count) return false;
        state.BigBoxStarTokens -= count;
        return true;
    }

    public bool UseMiniBoxTokens(int count)
    {
        if (state.MiniBoxTokens < count) return false;
        state.MiniBoxTokens -= count;
        return true;
    }

    public int GetPlayerLevel()
    {
        if (state.Experience < 40)
            return 1;

        var milestoneData = LogicDataTables.GetAllDataByClassId<LogicMilestoneData>(39)!;

        var maxLevel = 1;
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var level in milestoneData)
        {
            if (level.TypeInCsv != 5 ||
                level.ProgressStart + level.Progress > state.Experience) continue;

            if (level.Index + 2 > maxLevel)
                maxLevel = level.Index + 2;
        }

        return maxLevel;
    }

    public bool IsHeroUnlocked(int characterGlobalId, [NotNullWhen(true)] out HeroEntry? p1)
    {
        var snapshot = state.HeroEntries.ToArray();
        p1 = snapshot.FirstOrDefault(x => x.CharacterGlobalId == characterGlobalId);
        return p1 != null;
    }

    public async Task<int> EndClientTurnReceived(int tick, int checksum, List<(int, LogicCommand?)> commands)
    {
        foreach (var command in commands)
        {
            var res = await commandManager.ReceiveCommandAsync(command.Item2);

            if (res >= 0)
                continue;

            Logger.Error($"Failed to execute client command {command.Item1}. Error code: {res}");

            var r = await messageManager.SendMessagesAndDisconnectAsync(new OutOfSyncMessage
                { Capacity = 32 });

            return r ? 0 : -1;
        }

        return 0;
    }

    private int PityRollStarPower()
    {
        if (--state.StarPowerPityCounter > 0) return 0;
        state.StarPowerPityCounter = HomeSettings.GetConfig().GachaSystem.DefaultPityCounterForStarPower;

        return DropRandomAvailableForUnlockingStarPower();
    }

    private List<int> PityRollBrawlers(BoxType boxType)
    {
        var heroes = state.HeroEntries.ToArray();

        var animatedBrawlers = new List<int>();

        if (--state.ForcedDrops.PityCounters[0] <= 0) // rare
        {
            state.ForcedDrops.PityCounters[0] = HomeSettings.GetConfig().GachaSystem.DefaultPityCounterForRareBrawlers;

            var ownedBrawlerIds = new HashSet<int>(heroes.Select(h => h.CharacterGlobalId));

            var availableRareBrawlers = RareBrawlers.AsValueEnumerable()
                .Where(x => !ownedBrawlerIds.Contains(x.GlobalId))
                .ToArray();

            if (availableRareBrawlers.Length == 0)
                goto l2;

            var randomRareBrawlerIndex = Random.Shared.Next(availableRareBrawlers.Length);
            var randomRareBrawler = availableRareBrawlers[randomRareBrawlerIndex];

            animatedBrawlers.Add(randomRareBrawler.GlobalId);
            state.HeroEntries.Add(new HeroEntry(randomRareBrawler.GlobalId));

            if (boxType == BoxType.Mini)
                return animatedBrawlers;
        }

        l2:
        if (--state.ForcedDrops.PityCounters[1] <= 0) // super rare
        {
            state.ForcedDrops.PityCounters[1] =
                HomeSettings.GetConfig().GachaSystem.DefaultPityCounterForSuperRareBrawlers;

            var ownedBrawlerIds = new HashSet<int>(heroes.Select(h => h.CharacterGlobalId));

            var availableSuperRareBrawlers = SuperRareBrawlers.AsValueEnumerable()
                .Where(x => !ownedBrawlerIds.Contains(x.GlobalId))
                .ToArray();

            if (availableSuperRareBrawlers.Length == 0)
                goto l3;

            var randomSuperRareBrawlerIndex = Random.Shared.Next(availableSuperRareBrawlers.Length);
            var randomSuperRareBrawler = availableSuperRareBrawlers[randomSuperRareBrawlerIndex];

            animatedBrawlers.Add(randomSuperRareBrawler.GlobalId);
            state.HeroEntries.Add(new HeroEntry(randomSuperRareBrawler.GlobalId));

            if (boxType == BoxType.Mini)
                return animatedBrawlers;
        }

        l3:
        if (--state.ForcedDrops.PityCounters[2] <= 0) // epic
        {
            state.ForcedDrops.PityCounters[2] = HomeSettings.GetConfig().GachaSystem.DefaultPityCounterForEpicBrawlers;

            var ownedBrawlerIds = new HashSet<int>(heroes.Select(h => h.CharacterGlobalId));

            var availableEpicBrawlers = EpicBrawlers.AsValueEnumerable()
                .Where(x => !ownedBrawlerIds.Contains(x.GlobalId))
                .ToArray();

            if (availableEpicBrawlers.Length == 0)
                goto l4;

            var randomEpicBrawlerIndex = Random.Shared.Next(availableEpicBrawlers.Length);
            var randomEpicBrawler = availableEpicBrawlers[randomEpicBrawlerIndex];

            animatedBrawlers.Add(randomEpicBrawler.GlobalId);
            state.HeroEntries.Add(new HeroEntry(randomEpicBrawler.GlobalId));

            if (boxType == BoxType.Mini)
                return animatedBrawlers;

            if (boxType == BoxType.Big && animatedBrawlers.Count >= 3)
                return animatedBrawlers;
        }

        l4:
        if (--state.ForcedDrops.PityCounters[3] <= 0) // mythic
        {
            state.ForcedDrops.PityCounters[3] =
                HomeSettings.GetConfig().GachaSystem.DefaultPityCounterForMythicBrawlers;

            var ownedBrawlerIds = new HashSet<int>(heroes.Select(h => h.CharacterGlobalId));

            var availableMythicBrawlers = MythicBrawlers.AsValueEnumerable()
                .Where(x => !ownedBrawlerIds.Contains(x.GlobalId))
                .ToArray();

            if (availableMythicBrawlers.Length == 0)
                goto l5;

            var randomMythicBrawlerIndex = Random.Shared.Next(availableMythicBrawlers.Length);
            var randomMythicBrawler = availableMythicBrawlers[randomMythicBrawlerIndex];

            animatedBrawlers.Add(randomMythicBrawler.GlobalId);
            state.HeroEntries.Add(new HeroEntry(randomMythicBrawler.GlobalId));

            if (boxType == BoxType.Mini)
                return animatedBrawlers;

            if (boxType == BoxType.Big && animatedBrawlers.Count >= 3)
                return animatedBrawlers;
        }

        l5:
        if (--state.ForcedDrops.PityCounters[4] <= 0) // legendary
        {
            state.ForcedDrops.PityCounters[4] =
                HomeSettings.GetConfig().GachaSystem.DefaultPityCounterForLegendaryBrawlers;

            var ownedBrawlerIds = new HashSet<int>(heroes.Select(h => h.CharacterGlobalId));

            var availableLegendaryBrawlers = LegendaryBrawlers.AsValueEnumerable()
                .Where(x => !ownedBrawlerIds.Contains(x.GlobalId))
                .ToArray();

            if (availableLegendaryBrawlers.Length == 0)
                return animatedBrawlers;

            var randomLegendaryBrawlerIndex = Random.Shared.Next(availableLegendaryBrawlers.Length);
            var randomLegendaryBrawler = availableLegendaryBrawlers[randomLegendaryBrawlerIndex];

            animatedBrawlers.Add(randomLegendaryBrawler.GlobalId);
            state.HeroEntries.Add(new HeroEntry(randomLegendaryBrawler.GlobalId));

            if (boxType == BoxType.Mini)
                return animatedBrawlers;

            if (boxType == BoxType.Big && animatedBrawlers.Count >= 3)
                return animatedBrawlers;

            if (boxType == BoxType.Mega && animatedBrawlers.Count >= 5)
                return animatedBrawlers;
        }

        return animatedBrawlers;
    }

    private void TryAddBonus(List<GatchaDrop> gatchaDrops, BoxType boxType)
    {
        var multiplier = boxType switch
        {
            BoxType.Mini => 1,
            BoxType.Big => 2,
            _ => 3
        };

        if (Random.Shared.Next(0, 100) >= 7 + multiplier) return; // bonuses

        if (Random.Shared.Next(0, 100) <= 50 && state.Events.Any(x => x.Slot == 7))
            gatchaDrops.Add(new GatchaDrop { Type = 3, Count = Random.Shared.Next(3, 10) * multiplier }); // tickets
        else
            gatchaDrops.Add(new GatchaDrop { Type = 8, Count = Random.Shared.Next(2, 9) * multiplier }); // gems
    }

    private int DropRandomAvailableForUnlockingStarPower()
    {
        var heroesWithMaxLevel = state.HeroEntries
            .ToArray()
            .AsValueEnumerable()
            .Where(x => x.PowerLevel == 9);
        if (!heroesWithMaxLevel.Any()) return 0;

        var heroesWithoutStarPower = heroesWithMaxLevel
            .Where(x => x.StarPowersContainer.Count < 2)
            .ToArray();
        if (heroesWithoutStarPower.Length < 1) return 0;

        var randomHeroWithoutStarPowerIndex = Random.Shared.Next(heroesWithoutStarPower.Length);
        var randomHeroWithoutStarPower = heroesWithoutStarPower[randomHeroWithoutStarPowerIndex];

        var characterName = LogicDataTables.GetDataById(randomHeroWithoutStarPower.CharacterGlobalId)!.Name;

        var availableStarPowersForRandomHero = StarPowers.AsValueEnumerable()
            .Where(x => x.Target == characterName)
            .Where(x => !randomHeroWithoutStarPower.StarPowersContainer.ContainsKey(x.GlobalId))
            .ToArray();
        if (availableStarPowersForRandomHero.Length == 0) return 0;

        var randomStarPowerIndex = Random.Shared.Next(availableStarPowersForRandomHero.Length);
        var randomStarPower = availableStarPowersForRandomHero[randomStarPowerIndex];

        randomHeroWithoutStarPower.AddCard(randomStarPower.GlobalId);

        return randomStarPower.GlobalId;
    }

    private int TryAddStarPower()
    {
        if (Random.Shared.NextDouble() < state.StarPowerChance)
        {
            state.StarPowerChance = HomeSettings.GetConfig().GachaSystem.DefaultStarPowerChance;

            return DropRandomAvailableForUnlockingStarPower();
        }

        state.StarPowerChance += 0.00004f;
        return 0;
    }

    private int TryAddBrawler()
    {
        var heroes = state.HeroEntries.ToArray();

        if (Random.Shared.NextDouble() < state.LegendaryBrawlerChance)
        {
            state.LegendaryBrawlerChance = HomeSettings.GetConfig().GachaSystem.DefaultLegendaryBrawlerChance;

            var ownedBrawlerIds = new HashSet<int>(heroes.Select(h => h.CharacterGlobalId));

            var availableLegendaryBrawlers = LegendaryBrawlers.AsValueEnumerable()
                .Where(x => !ownedBrawlerIds.Contains(x.GlobalId))
                .ToArray();

            if (availableLegendaryBrawlers.Length == 0)
                return 0;

            var randomLegendaryBrawlerIndex = Random.Shared.Next(availableLegendaryBrawlers.Length);
            var randomLegendaryBrawler = availableLegendaryBrawlers[randomLegendaryBrawlerIndex];

            state.HeroEntries.Add(new HeroEntry(randomLegendaryBrawler.GlobalId));
            return randomLegendaryBrawler.GlobalId;
        }

        state.LegendaryBrawlerChance += 0.00000393f;
        state.LegendaryBrawlerChance = Math.Min(state.LegendaryBrawlerChance, 0.01f);

        if (Random.Shared.NextDouble() < state.MythicBrawlerChance)
        {
            state.MythicBrawlerChance = HomeSettings.GetConfig().GachaSystem.DefaultMythicBrawlerChance;

            var ownedBrawlerIds = new HashSet<int>(heroes.Select(h => h.CharacterGlobalId));

            var availableMythicBrawlers = MythicBrawlers.AsValueEnumerable()
                .Where(x => !ownedBrawlerIds.Contains(x.GlobalId))
                .ToArray();

            if (availableMythicBrawlers.Length == 0)
                return 0;

            var randomMythicBrawlerIndex = Random.Shared.Next(availableMythicBrawlers.Length);
            var randomMythicBrawler = availableMythicBrawlers[randomMythicBrawlerIndex];

            state.HeroEntries.Add(new HeroEntry(randomMythicBrawler.GlobalId));
            return randomMythicBrawler.GlobalId;
        }

        state.MythicBrawlerChance += 0.000008389f;
        state.MythicBrawlerChance = Math.Min(state.MythicBrawlerChance, 0.02f);

        if (Random.Shared.NextDouble() < state.EpicBrawlerChance)
        {
            state.EpicBrawlerChance = HomeSettings.GetConfig().GachaSystem.DefaultEpicBrawlerChance;

            var ownedBrawlerIds = new HashSet<int>(heroes.Select(h => h.CharacterGlobalId));

            var availableEpicBrawlers = EpicBrawlers.AsValueEnumerable()
                .Where(x => !ownedBrawlerIds.Contains(x.GlobalId))
                .ToArray();

            if (availableEpicBrawlers.Length == 0)
                return 0;

            var randomEpicBrawlerIndex = Random.Shared.Next(availableEpicBrawlers.Length);
            var randomEpicBrawler = availableEpicBrawlers[randomEpicBrawlerIndex];

            state.HeroEntries.Add(new HeroEntry(randomEpicBrawler.GlobalId));
            return randomEpicBrawler.GlobalId;
        }

        state.EpicBrawlerChance += 0.00009359f;
        state.EpicBrawlerChance = Math.Min(state.EpicBrawlerChance, 0.03f);

        if (Random.Shared.NextDouble() < state.SuperRareBrawlerChance)
        {
            state.SuperRareBrawlerChance = HomeSettings.GetConfig().GachaSystem.DefaultSuperRareBrawlerChance;

            var ownedBrawlerIds = new HashSet<int>(heroes.Select(h => h.CharacterGlobalId));

            var availableSuperRareBrawlers = SuperRareBrawlers.AsValueEnumerable()
                .Where(x => !ownedBrawlerIds.Contains(x.GlobalId))
                .ToArray();

            if (availableSuperRareBrawlers.Length == 0)
                return 0;

            var randomSuperRareBrawlerIndex = Random.Shared.Next(availableSuperRareBrawlers.Length);
            var randomSuperRareBrawler = availableSuperRareBrawlers[randomSuperRareBrawlerIndex];

            state.HeroEntries.Add(new HeroEntry(randomSuperRareBrawler.GlobalId));
            return randomSuperRareBrawler.GlobalId;
        }

        state.SuperRareBrawlerChance += 0.001101f;
        state.SuperRareBrawlerChance = Math.Min(state.SuperRareBrawlerChance, 0.09f);

        if (Random.Shared.NextDouble() < state.RareBrawlerChance)
        {
            state.RareBrawlerChance = HomeSettings.GetConfig().GachaSystem.DefaultRareBrawlerChance;

            var ownedBrawlerIds = new HashSet<int>(heroes.Select(h => h.CharacterGlobalId));

            var availableRareBrawlers = RareBrawlers.AsValueEnumerable()
                .Where(x => !ownedBrawlerIds.Contains(x.GlobalId))
                .ToArray();

            if (availableRareBrawlers.Length == 0)
                return 0;

            var randomRareBrawlerIndex = Random.Shared.Next(availableRareBrawlers.Length);
            var randomRareBrawler = availableRareBrawlers[randomRareBrawlerIndex];

            state.HeroEntries.Add(new HeroEntry(randomRareBrawler.GlobalId));
            return randomRareBrawler.GlobalId;
        }

        state.RareBrawlerChance += 0.0021001f;
        state.RareBrawlerChance = Math.Min(state.RareBrawlerChance, 0.1f);
        return 0;
    }

    public IEnumerable<DeliveryUnit> OpenBoxes(BoxType boxType, int count)
    {
        for (var i = 0; i < count; i++)
            yield return OpenBox(boxType);
    }

    public IEnumerable<DeliveryUnit> OpenBoxes(int itemType, int count)
    {
        var boxType = itemType switch
        {
            0 or 2 or 6 => BoxType.Mini,
            14 => BoxType.Big,
            10 => BoxType.Mega,
            _ => BoxType.Mini
        };

        for (var i = 0; i < count; i++)
            yield return OpenBox(boxType);
    }

    private DeliveryUnit OpenBox(BoxType boxType)
    {
        var heroes = state.HeroEntries.ToArray();
        var gatchaDrops = new List<GatchaDrop>();

        switch (boxType)
        {
            case BoxType.Mini:
            {
                var pityStarPower = PityRollStarPower();

                if (pityStarPower != 0)
                {
                    gatchaDrops.Add(new GatchaDrop { Type = 4, Count = 1, CardGlobalId = pityStarPower });
                    break;
                }

                var pityBrawlers = PityRollBrawlers(boxType);

                // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                foreach (var pityBrawler in pityBrawlers)
                    gatchaDrops.Add(new GatchaDrop { Type = 1, Count = 1, HeroGlobalId = pityBrawler });

                if (pityBrawlers.Count > 0)
                    break;

                var starPower = TryAddStarPower();

                if (starPower != 0)
                {
                    gatchaDrops.Add(new GatchaDrop { Type = 4, Count = 1, CardGlobalId = starPower });
                    break;
                }

                var brawler = TryAddBrawler();

                if (brawler != 0)
                {
                    gatchaDrops.Add(new GatchaDrop { Type = 1, Count = 1, HeroGlobalId = brawler });
                    break;
                }

                var moneysMultiplier =
                    heroes.AsValueEnumerable().All(x => x.PowerPoints >= FullBrawlerPoints) ? 2 : 1;
                var moneys = Random.Shared.Next(5, 30) * moneysMultiplier;

                gatchaDrops.Add(new GatchaDrop { Type = 7, Count = moneys });

                var heroesWithoutFullPowerPoints =
                    heroes.AsValueEnumerable()
                        .Where(x => x.PowerPoints < FullBrawlerPoints)
                        .OrderBy(_ => Random.Shared.Next())
                        .ToArray();

                var powerPointsMax = 2;
                var powerPoints = 0;

                foreach (var hero in heroesWithoutFullPowerPoints)
                {
                    var powerPointsAmount = Random.Shared.Next(powerPointsMax + 1, powerPointsMax + 7);
                    powerPointsMax = powerPointsAmount;

                    var count = Math.Min(powerPointsAmount, FullBrawlerPoints - hero.PowerPoints);
                    gatchaDrops.Add(new GatchaDrop { Type = 6, Count = count, HeroGlobalId = hero.CharacterGlobalId });
                    hero.PowerPoints += count;

                    if (++powerPoints == 2) break;
                }

                TryAddBonus(gatchaDrops, boxType);
                break;
            }
            case BoxType.Big:
            {
                var pityBrawlers = PityRollBrawlers(boxType);

                // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                foreach (var pityBrawler in pityBrawlers)
                    gatchaDrops.Add(new GatchaDrop { Type = 1, Count = 1, HeroGlobalId = pityBrawler });

                if (gatchaDrops.Count >= 3)
                    break;

                var pityStarPower = PityRollStarPower();

                if (pityStarPower != 0)
                    gatchaDrops.Add(new GatchaDrop { Type = 4, Count = 1, CardGlobalId = pityStarPower });

                if (gatchaDrops.Count >= 3)
                    break;

                var starPower = TryAddStarPower();

                if (starPower != 0)
                    gatchaDrops.Add(new GatchaDrop { Type = 4, Count = 1, CardGlobalId = starPower });

                if (gatchaDrops.Count >= 3)
                    break;

                var brawler = TryAddBrawler();

                if (brawler != 0)
                    gatchaDrops.Add(new GatchaDrop { Type = 1, Count = 1, HeroGlobalId = brawler });

                if (gatchaDrops.Count >= 3)
                    break;

                var brawler2 = TryAddBrawler();

                if (brawler2 != 0)
                    gatchaDrops.Add(new GatchaDrop { Type = 1, Count = 1, HeroGlobalId = brawler2 });

                if (gatchaDrops.Count >= 3)
                    break;

                var maxMoneys = gatchaDrops.Count switch
                {
                    0 => 90,
                    1 => 49,
                    2 => 32,
                    _ => 0
                };

                if (maxMoneys == 0)
                    break;

                var moneysMultiplier =
                    heroes.AsValueEnumerable().All(x => x.PowerPoints >= FullBrawlerPoints) ? 2 : 1;
                var moneys = Random.Shared.Next(maxMoneys / 2, maxMoneys) * moneysMultiplier;

                gatchaDrops.Insert(0, new GatchaDrop { Type = 7, Count = moneys });

                var heroesWithoutFullPowerPoints =
                    heroes.AsValueEnumerable()
                        .Where(x => x.PowerPoints < FullBrawlerPoints)
                        .Where(x => !pityBrawlers.Contains(x.CharacterGlobalId) &&
                                    x.CharacterGlobalId != brawler &&
                                    x.CharacterGlobalId != brawler2)
                        .OrderBy(_ => Random.Shared.Next())
                        .ToArray();

                var powerPointsMax = 6;
                var powerPoints = 0;

                foreach (var hero in heroesWithoutFullPowerPoints)
                {
                    if (gatchaDrops.Count >= 4) break;

                    var powerPointsAmount = Random.Shared.Next(powerPointsMax + 2, powerPointsMax + 7);
                    powerPointsMax = powerPointsAmount;

                    var count = Math.Min(powerPointsAmount, FullBrawlerPoints - hero.PowerPoints);
                    gatchaDrops.Insert(++powerPoints,
                        new GatchaDrop { Type = 6, Count = count, HeroGlobalId = hero.CharacterGlobalId });
                    hero.PowerPoints += count;
                }

                TryAddBonus(gatchaDrops, boxType);
                break;
            }
            case BoxType.Mega:
            {
                var pityBrawlers = PityRollBrawlers(boxType);

                // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                foreach (var pityBrawler in pityBrawlers)
                    gatchaDrops.Add(new GatchaDrop { Type = 1, Count = 1, HeroGlobalId = pityBrawler });

                var pityStarPower = PityRollStarPower();

                if (pityStarPower != 0)
                    gatchaDrops.Add(new GatchaDrop { Type = 4, Count = 1, CardGlobalId = pityStarPower });

                var starPower = TryAddStarPower();

                if (starPower != 0)
                    gatchaDrops.Add(new GatchaDrop { Type = 4, Count = 1, CardGlobalId = starPower });

                var brawler = TryAddBrawler();

                if (brawler != 0)
                    gatchaDrops.Add(new GatchaDrop { Type = 1, Count = 1, HeroGlobalId = brawler });

                var brawler2 = TryAddBrawler();

                if (brawler2 != 0)
                    gatchaDrops.Add(new GatchaDrop { Type = 1, Count = 1, HeroGlobalId = brawler2 });

                var brawler3 = TryAddBrawler();

                if (brawler3 != 0)
                    gatchaDrops.Add(new GatchaDrop { Type = 1, Count = 1, HeroGlobalId = brawler3 });

                var maxMoneys = gatchaDrops.Count switch
                {
                    0 => 226,
                    1 => 185,
                    2 => 120,
                    3 => 100,
                    4 => 94,
                    5 => 84,
                    6 => 52,
                    7 => 36,
                    8 => 20,
                    9 => 12,
                    10 => 4,
                    _ => 0
                };

                if (maxMoneys == 0)
                    break;

                var moneysMultiplier =
                    heroes.AsValueEnumerable().All(x => x.PowerPoints >= FullBrawlerPoints) ? 2 : 1;
                var moneys = Random.Shared.Next(maxMoneys / 2, maxMoneys) * moneysMultiplier;

                gatchaDrops.Insert(0, new GatchaDrop { Type = 7, Count = moneys });

                var heroesWithoutFullPowerPoints =
                    heroes.AsValueEnumerable()
                        .Where(x => x.PowerPoints < FullBrawlerPoints)
                        .Where(x => !pityBrawlers.Contains(x.CharacterGlobalId) &&
                                    x.CharacterGlobalId != brawler &&
                                    x.CharacterGlobalId != brawler2 &&
                                    x.CharacterGlobalId != brawler3)
                        .OrderBy(_ => Random.Shared.Next())
                        .ToArray();

                var powerPointsMax = 20;
                var powerPoints = 0;

                foreach (var hero in heroesWithoutFullPowerPoints)
                {
                    var powerPointsAmount = Random.Shared.Next(powerPointsMax + 2, powerPointsMax + 7);
                    powerPointsMax = powerPointsAmount;

                    var count = Math.Min(powerPointsAmount, FullBrawlerPoints - hero.PowerPoints);
                    gatchaDrops.Insert(++powerPoints,
                        new GatchaDrop { Type = 6, Count = count, HeroGlobalId = hero.CharacterGlobalId });
                    hero.PowerPoints += count;

                    if (powerPoints == 5) break;
                }

                TryAddBonus(gatchaDrops, boxType);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(boxType), boxType, null);
        }

        var unitType = boxType switch
        {
            BoxType.Mini => 10,
            BoxType.Big => 12,
            BoxType.Mega => 11,
            _ => throw new ArgumentOutOfRangeException(nameof(boxType), boxType, null)
        };

        return new DeliveryUnit { Type = unitType, GatchaDrops = gatchaDrops };
    }

    public List<DeliveryUnit>? GetDeliveryUnits(LogicGemOffer gemOffer, int selectedRewardGlobalGid)
    {
        var u = GetDeliveryUnitsByShopItem((ShopItemHelperTable)gemOffer.Type, gemOffer.Count, gemOffer.ItemGlobalId,
            selectedRewardGlobalGid, 0);

        return u.needToTryDoubleReward ? null : u.deliveryUnits;
    }

    public List<DeliveryUnit>? GetDeliveryUnits(LogicMilestoneData logicMilestoneData, int selectedRewardGlobalGid)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var doubleReward = attempt == 1;

            var rewardType = doubleReward
                ? logicMilestoneData.SecondaryLvlUpRewardType
                : logicMilestoneData.PrimaryLvlUpRewardType;

            var rewardHeroData = doubleReward
                ? logicMilestoneData.SecondaryLvlUpRewardHero
                : logicMilestoneData.PrimaryLvlUpRewardHero;

            var rewardExtraData = doubleReward
                ? logicMilestoneData.SecondaryLvlUpRewardExtraData
                : logicMilestoneData.PrimaryLvlUpRewardExtraData;

            var rewardCount = doubleReward
                ? logicMilestoneData.SecondaryLvlUpRewardCount
                : logicMilestoneData.PrimaryLvlUpRewardCount;

            if (rewardType == -1) continue;

            int? heroGlobalId = 0;
            if (!string.IsNullOrWhiteSpace(rewardHeroData))
                heroGlobalId = LogicDataTables.GetDataByName(16, rewardHeroData)?.GlobalId;

            var (deliveryUnits, needRetry) =
                GetDeliveryUnitsByShopItem((ShopItemHelperTable)rewardType, rewardCount,
                    heroGlobalId, selectedRewardGlobalGid, rewardExtraData);

            if (!needRetry)
                return deliveryUnits;
        }

        return null;
    }

    private (List<DeliveryUnit> deliveryUnits, bool needToTryDoubleReward)
        GetDeliveryUnitsByShopItem(ShopItemHelperTable itemType, int count,
            int? heroOrSkinOrStarPowerGlobalId, int selectedRewardGlobalGid, int rewardExtraData)
    {
        var deliveryUnits = new List<DeliveryUnit>();

        if (itemType.IsBox())
            return (OpenBoxes((int)itemType, count).ToList(), false);

        switch (itemType)
        {
            case ShopItemHelperTable.Gems:
                deliveryUnits.Add(new DeliveryUnit
                    { Type = 100, GatchaDrops = [new GatchaDrop { Type = 8, Count = count }] });
                break;

            case ShopItemHelperTable.UpgradeMaterial:
                deliveryUnits.Add(new DeliveryUnit
                    { Type = 100, GatchaDrops = [new GatchaDrop { Type = 7, Count = count }] });
                break;

            case ShopItemHelperTable.Ticket:
                deliveryUnits.Add(new DeliveryUnit
                    { Type = 100, GatchaDrops = [new GatchaDrop { Type = 3, Count = count }] });
                break;

            case ShopItemHelperTable.StarPoints:
                deliveryUnits.Add(new DeliveryUnit
                    { Type = 100, GatchaDrops = [new GatchaDrop { Type = 12, Count = count }] });
                break;

            case ShopItemHelperTable.CoinDoubler:
                deliveryUnits.Add(new DeliveryUnit
                    { Type = 100, GatchaDrops = [new GatchaDrop { Type = 2, Count = count }] });
                break;

            case ShopItemHelperTable.GuaranteedHero:
            {
                if (heroOrSkinOrStarPowerGlobalId == null)
                    return ([], true);

                var character = LogicDataTables.GetDataById<LogicCharacterData>(heroOrSkinOrStarPowerGlobalId.Value);

                if (character == null || IsHeroUnlocked(character.GlobalId, out _))
                    return ([], true);

                deliveryUnits.Add(new DeliveryUnit
                {
                    Type = 100,
                    GatchaDrops = [new GatchaDrop { Type = 1, Count = 1, HeroGlobalId = character.GlobalId }]
                });

                state.HeroEntries.Add(new HeroEntry(character.GlobalId));
                break;
            }

            case ShopItemHelperTable.Skin:
            case ShopItemHelperTable.SkinAndHero:
            {
                if (heroOrSkinOrStarPowerGlobalId == null)
                    return ([], true);

                var skin = LogicDataTables.GetDataById<LogicSkinData>(heroOrSkinOrStarPowerGlobalId.Value);
                if (skin == null)
                    return ([], true);

                var character = SkinIdToCharacterData.GetValueOrDefault(skin.GlobalId);
                if (character == null)
                    return ([], true);

                if (state.UnlockedSkins.Contains(skin.GlobalId))
                    return ([], true);

                if (!IsHeroUnlocked(character.GlobalId, out _))
                {
                    deliveryUnits.Add(new DeliveryUnit
                    {
                        Type = 100,
                        GatchaDrops = [new GatchaDrop { Type = 1, Count = 1, HeroGlobalId = character.GlobalId }]
                    });

                    state.HeroEntries.Add(new HeroEntry(character.GlobalId));
                }

                deliveryUnits.Add(new DeliveryUnit
                {
                    Type = 100,
                    GatchaDrops = [new GatchaDrop { Type = 9, Count = 1, SkinGlobalId = skin.GlobalId }]
                });

                state.UnlockedSkins.Add(skin.GlobalId);
                break;
            }

            case ShopItemHelperTable.WildcardPower:
            {
                if (state.HeroEntries.ToArray().All(x => x.PowerPoints >= FullBrawlerPoints))
                    return ([], true);

                var character = LogicDataTables.GetDataById<LogicCharacterData>(selectedRewardGlobalGid);
                if (character == null)
                    return ([], true);

                if (!IsHeroUnlocked(character.GlobalId, out var hero))
                    break;

                var delta = LogicMath.Min(FullBrawlerPoints - hero.PowerPoints, count);

                deliveryUnits.Add(new DeliveryUnit
                {
                    Type = 100,
                    GatchaDrops = [new GatchaDrop { Type = 6, Count = delta, HeroGlobalId = character.GlobalId }]
                });
                hero.PowerPoints += count;

                if (count != delta)
                    deliveryUnits.Add(new DeliveryUnit
                        { Type = 100, GatchaDrops = [new GatchaDrop { Type = 7, Count = (count - delta) * 2 }] });
                break;
            }

            case ShopItemHelperTable.HeroPower:
            {
                if (heroOrSkinOrStarPowerGlobalId == null)
                    return ([], true);

                var character = LogicDataTables.GetDataById<LogicCharacterData>(heroOrSkinOrStarPowerGlobalId.Value);
                if (character == null)
                    return ([], true);

                if (!IsHeroUnlocked(character.GlobalId, out var hero))
                    break;

                var delta = LogicMath.Min(FullBrawlerPoints - hero.PowerPoints, count);

                deliveryUnits.Add(new DeliveryUnit
                {
                    Type = 100,
                    GatchaDrops = [new GatchaDrop { Type = 6, Count = delta, HeroGlobalId = character.GlobalId }]
                });
                hero.PowerPoints += count;

                if (count != delta)
                    deliveryUnits.Add(new DeliveryUnit
                        { Type = 100, GatchaDrops = [new GatchaDrop { Type = 7, Count = (count - delta) * 2 }] });
                break;
            }

            case ShopItemHelperTable.Item:
            {
                if (heroOrSkinOrStarPowerGlobalId == null)
                    return ([], true);

                var reward = heroOrSkinOrStarPowerGlobalId.Value;

                var card = LogicDataTables.GetDataById<LogicCardData>(reward);
                if (card == null)
                    return ([], true);

                var character = LogicDataTables.GetDataByName<LogicCharacterData>(card.Target);
                if (character == null)
                    return ([], true);

                if (!IsHeroUnlocked(character.GlobalId, out var hero))
                    return ([], true);

                if (hero.StarPowersContainer.ContainsKey(reward))
                    return ([], true);

                deliveryUnits.Add(
                    new DeliveryUnit
                        { Type = 100, GatchaDrops = [new GatchaDrop { Type = 4, Count = 0, CardGlobalId = reward }] });
                hero.AddCard(reward);
                break;
            }

            case ShopItemHelperTable.EventSlot:
            {
                var eventSlot = state.EventSlots.Find(x => x.Slot == rewardExtraData);

                if (eventSlot == null)
                {
                    state.EventSlots.Add(new EventSlot { Slot = rewardExtraData, Unlocked = true });
                    break;
                }

                if (eventSlot.Unlocked)
                    return ([], true);

                eventSlot.Unlocked = true;

                if (eventSlot.Slot is 2 or 5)
                    foreach (var slot in state.EventSlots)
                        if (slot.Slot is 2 or 5)
                            slot.Unlocked = true;

                break;
            }
            default:
                throw new ArgumentOutOfRangeException($"{itemType}: Milestone unparsed exception!");
        }

        return (deliveryUnits, false);
    }
}