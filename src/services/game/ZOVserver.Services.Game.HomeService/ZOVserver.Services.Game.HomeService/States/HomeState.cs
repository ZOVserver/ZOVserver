using MessagePack;
using ZOVserver.Shared.Contracts.Laser.Combined.Entries;
using ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;
using ZOVserver.Shared.Contracts.Laser.Combined.Notifications;

namespace ZOVserver.Services.Game.HomeService.States;

[MessagePackObject]
public class HomeState
{
    [Key(24)] public string SupportedContentCreator = string.Empty;
    [Key(0)] public int GameState { get; set; }
    [Key(1)] public Guid SessionId { get; set; }
    [Key(2)] public long AccountId { get; set; }
    [Key(3)] public DateTime LastKeepAliveReceivedTime { get; set; }

    [Key(4)] public long HomeId { get; set; }

    [Key(5)] public int NowTrophies { get; set; }

    [Key(6)] public int MaxTrophies { get; set; }

    [Key(7)] public int TokenDoublerCount { get; set; }

    [Key(8)] public int Unused1 { get; set; }

    [Key(9)] public int NextNameChangePrice { get; set; }

    [Key(10)] public DateTime NextNameChangeTime { get; set; }

    [Key(11)] public int TrophyRoadProgress { get; set; }

    [Key(12)] public int Experience { get; set; }

    [Key(13)] public int ThumbnailGlobalId { get; set; }

    [Key(14)] public int NameColorGlobalId { get; set; }

    [Key(15)] public List<int> UnlockedSkins { get; set; } = [];

    [Key(16)] public List<int> SelectedSkins { get; set; } = [];

    [Key(17)] public bool TokenLimitReached { get; set; }

    [Key(18)] public List<LogicOfferBundles> OfferBundles { get; set; } = [];

    [Key(19)] public int AvailableBattleTokens { get; set; }

    [Key(20)] public DateTime NextBattleTokensTime { get; set; }

    [Key(21)] public int Tickets { get; set; }

    [Key(22)] public int HomeBrawlerGlobalId { get; set; }

    [Key(23)] public string Region { get; set; } = string.Empty;

    [Key(25)] public List<IntValueEntry> IntValuesDaily { get; set; } = [];

    [Key(26)] public List<int> StarBrawlers { get; set; } = [];

    [Key(27)] public List<int> TicketPurchasedIndexes { get; set; } = [];

    [Key(28)] public List<EventData> Events { get; set; } = [];

    [Key(29)] public bool ForceTicketsEventEnable { get; set; }

    [Key(30)] public int MyLastSeasonCounter { get; set; }

    [Key(31)] public List<ReleaseEntry> ReleaseEntries { get; set; } = [];

    [Key(32)] public int LobbyTheme { get; set; }

    [Key(33)] public string AvatarName { get; set; } = string.Empty;

    [Key(34)] public bool NameSetByUser { get; set; }

    [Key(35)] public int NumberOfNameChanges { get; set; }

    [Key(36)] public List<HeroEntry> HeroEntries { get; set; } = [];

    [Key(37)] public int MiniBoxTokens { get; set; }

    [Key(38)] public int BigBoxStarTokens { get; set; }

    [Key(39)] public int Gold { get; set; }

    [Key(40)] public int Diamonds { get; set; }

    [Key(41)] public int StarPoints { get; set; }

    [Key(42)] public int TutorialState { get; set; }

    [Key(43)] public bool BlockInvites { get; set; }

    [Key(45)] public int GoodDropFractal { get; set; }

    [Key(46)] public ForcedDrops ForcedDrops { get; set; } = new();
    [Key(47)] public int StarPowerPityCounter { get; set; }

    [Key(48)] public float RareBrawlerChance { get; set; }
    [Key(49)] public float SuperRareBrawlerChance { get; set; }
    [Key(50)] public float EpicBrawlerChance { get; set; }
    [Key(51)] public float MythicBrawlerChance { get; set; }
    [Key(52)] public float LegendaryBrawlerChance { get; set; }
    [Key(53)] public float StarPowerChance { get; set; }

    [Key(54)] public List<EventSlot> EventSlots { get; set; } = [];

    [Key(55)] public DateTime LastUpdateOffersTime { get; set; }

    [Key(56)] public DateTime HomeCreatedTime { get; set; }

    [Key(57)] public Dictionary<int, BaseNotification> Notifications { get; set; } = new();

    [IgnoreMember] public int PlayerStatus { get; set; }

    [Key(59)] public DateTime LastCountryChangeTime { get; set; }

    [Key(60)] public long AllianceId { get; set; }
    [Key(61)] public DateTime LastAllianceIdChangeTime { get; set; }

    [Key(62)] public int TrioWins { get; set; }
    [Key(63)] public int SoloWins { get; set; }
    [Key(64)] public int DuoWins { get; set; }

    [Key(65)] public int BestRoboRumbleTime { get; set; }
    [Key(66)] public int BestRaidBossLevel { get; set; }
    [Key(67)] public int BestBigGameBossTime { get; set; }
    [Key(68)] public int BestRankInLeague { get; set; }

    [Key(69)] public long TeamId { get; set; }

    [Key(70)] public Guid? BattleId { get; set; }

    [Key(71)] public HashSet<Guid> RemovedCustomOffers { get; set; } = [];

    [IgnoreMember] public int TeamEventSlot { get; set; }

    [IgnoreMember] public int MiniBoxOhdTokens { get; set; }
    [IgnoreMember] public int BigBoxOhdTokens { get; set; }
    [IgnoreMember] public int TrophiesOhd { get; set; }
    [IgnoreMember] public int LegendaryTrophiesOhd { get; set; }
}