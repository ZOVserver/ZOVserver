using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Entries;
using ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA;

[LaserSerializable]
public partial class LogicDailyData : LaserContract
{
    [Field(2, IsVarInt = true, DuplicateAfterOrders = [3])]
    public int NowTrophies { get; set; }

    [Field(3, IsVarInt = true, DuplicateAfterOrders = [12])]
    public int MaxTrophies { get; set; }

    [Field(16, IsVarInt = true)] public int TokenDoublerCount { get; set; }

    [Field(17, IsVarInt = true)] public int SecondsToSeasonEnd { get; set; }

    [Field(18, IsVarInt = true)] public int Unk100 { get; set; }

    [Field(19, IsVarInt = true)] public int Unk104 { get; set; }

    [Field(28, IsVarInt = true)] public int NameChangePrice { get; set; }

    [Field(29, IsVarInt = true)] public int SecondsToNextNameChange { get; set; }

    [Field(4, IsVarInt = true, AddNumber = 1)]
    public int TrophyRoadProgress { get; set; }

    [Field(5, IsVarInt = true)] public int Experience { get; set; }

    [Field(6)] public int ThumbnailGlobalId { get; set; }

    [Field(7)] public int NameColorGlobalId { get; set; }

    [Field(8, IsVarInt = true)] public int[] UnkArray36 { get; set; } = [];

    [Field(9, AsDataRef = true)] public List<int> SelectedSkins { get; set; } = [];

    [Field(10, AsDataRef = true)] public List<int> UnlockedSkins { get; set; } = [];

    [Field(11, IsVarInt = true)] public int Unk76 { get; set; }

    [Field(12, IsVarInt = true)] public int Unk80 { get; set; } = 1;

    [Field(13)] public bool TokenLimitReached { get; set; }

    [Field(14, IsVarInt = true)] public int Unk88 { get; set; } = 1;

    [Field(15)] public bool Unk72 { get; set; } = true;

    [Field(30)] public List<LogicOfferBundles> OfferBundles { get; set; } = [];

    [Field(31)] public List<AdStatus> AdStatuses { get; set; } = [];

    [Field(32, IsVarInt = true)] public int AvailableBattleTokens { get; set; }

    [Field(33, IsVarInt = true)] public int SecondsToNextBattleTokens { get; set; }

    [Field(35, IsVarInt = true)] public int Tickets { get; set; }

    [Field(36, IsVarInt = true)] public int Unk176 { get; set; }

    [Field(37)] public int HomeBrawlerGlobalId { get; set; }

    [Field(38)] public string Region { get; set; } = string.Empty;

    [Field(39)] public string SupportedContentCreator { get; set; } = string.Empty;

    // Key values:
    // 3 = mini box tokens,
    // 4 = trophies,
    // 5 = big box tokens,
    // 6 = unlocked all events and demo account,
    // 7 = invites blocked state,
    // 8 = legendary trophies,
    // 10 = power league trophies,
    // 13 = prototype of big box tokens.
    [Field(40)] public List<IntValueEntry> IntValues { get; set; } = [];

    [Field(41)] public List<CooldownEntry> Coldowns { get; set; } = [];

    [Field(42)] public List<Sub1Aa808>? UnkSub1 { get; set; }

    [Field(43)] public List<ProLeagueSeasonData> ProLeagueSeasonDatas { get; set; } = [];

    [Field(44, AsDataRef = true)] public List<int> StarBrawlers { get; set; } = [];

    [Field(34, IsVarInt = true)] public List<int> TicketPurchasedIndexes { get; set; } = [];

    [Field(20)] public ForcedDrops? ForcedDrops { get; set; }

    [Field(0, IsVarInt = true)] public DateTime Unk0 { get; set; } = DateTime.UtcNow;

    [Field(1, IsVarInt = true)] public DateTime Unk4 { get; set; } = DateTime.UtcNow;

    [Field(21)] public bool Unk73 { get; set; }

    [Field(22, PresenceBool = true)] public TimedOffer? Unk112TimedOffer { get; set; }

    [Field(23, PresenceBool = true)] public TimedOffer? Unk116TimedOffer { get; set; }

    [Field(24)] public bool TokenDoublerAvailableInShop { get; set; } = true;

    [Field(25, IsVarInt = true)] public int TokenDoublerNtState { get; set; } = 2;

    [Field(26, IsVarInt = true)] public int EventTicketsNtState { get; set; } = 2;

    [Field(27, IsVarInt = true)] public int CoinPacksNtState { get; set; } = 2;
}