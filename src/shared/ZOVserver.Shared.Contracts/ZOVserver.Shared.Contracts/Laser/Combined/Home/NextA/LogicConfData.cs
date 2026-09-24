using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Entries;
using ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA;

[LaserSerializable]
public partial class LogicConfData : LaserContract
{
    public const int MaxBattleTokens = 200;
    public const int PlusBattleTokens = 20;
    public const int BattleTokensRegenIntervalMinutes = 120;

    public const int TokenDoublerCost = 50;
    public const int TokenDoublerAmount = 1000;

    public static readonly int[] TicketPacksCost = [10, 30, 80];
    public static readonly int[] TicketsAmountInPacks = [6, 20, 60];

    public static readonly int[] GoldPacksCost = [20, 50, 140, 280];
    public static readonly int[] GoldAmountInPacks = [150, 400, 1200, 2600];

    public static readonly int[] GoldToNextHeroLevel = [20, 35, 75, 140, 290, 480, 800, 1250];
    public static readonly int[] PowerPointsToNextHeroLevel = [20, 30, 50, 80, 130, 210, 340, 550];

    public static readonly int[] BrawlersCost = [0, 30, 80, 170, 350, 700];

    public static readonly int[] Unk108Array = [1, 2, 3, 4, 5, 10, 15, 20];

    [Field(0, IsVarInt = true)] public DateTime Unk0 { get; set; } = DateTime.UtcNow;

    [Field(1, IsVarInt = true)] public int MiniBoxCost { get; set; } = 100;

    [Field(2, IsVarInt = true)] public int Unk8 { get; set; }

    [Field(3, IsVarInt = true)] public int BigBoxCost { get; set; } = 30;

    [Field(4, IsVarInt = true)] public int Unk16 { get; set; }

    [Field(5, IsVarInt = true)] public int MegaBoxCost { get; set; } = 80;

    [Field(6, IsVarInt = true)] public int Unk24 { get; set; }

    [Field(7, IsVarInt = true)] public int TokenDoublerCostS { get; set; } = TokenDoublerCost;

    [Field(8, IsVarInt = true)] public int TokenDoublerAmountS { get; set; } = TokenDoublerAmount;

    [Field(9, IsVarInt = true)] public int MinBrawlerTrophiesForSeasonReset { get; set; } = 500;

    [Field(10, IsVarInt = true)] public int TrophyLossPercentageInSeasonReset { get; set; } = 50;

    [Field(11, IsVarInt = true)] public int MiniBoxMaxTokensInEventAnimation { get; set; } = 9999900;

    [Field(12, IsVarInt = true)] public int[] BrawlersCostInGems { get; set; } = BrawlersCost;

    [Field(13)] public List<EventSlot> EventSlots { get; set; } = [];

    [Field(14, UseCustomContract = true)] public List<EventData> Events { get; set; } = [];

    [Field(15, UseCustomContract = true)] public List<EventData> UpcomingEvents { get; set; } = [];

    [Field(16, IsVarInt = true)] public int[] GoldToNextHeroLevelS { get; set; } = GoldToNextHeroLevel;

    [Field(17, IsVarInt = true)] public int[] Unk108ArrayS { get; set; } = Unk108Array;

    [Field(18, IsVarInt = true)] public int[] TicketPacksCostS { get; set; } = TicketPacksCost;

    [Field(19, IsVarInt = true)] public int[] TicketsAmountInPacksS { get; set; } = TicketsAmountInPacks;

    [Field(20, IsVarInt = true)] public int[] GoldPacksCostS { get; set; } = GoldPacksCost;

    [Field(22, IsVarInt = true)] public int[] GoldAmountInPacksS { get; set; } = GoldAmountInPacks;

    [Field(23, IsVarInt = true)] public int Unk168 { get; set; }

    [Field(24, IsVarInt = true)] public int MaxBattleTokensS { get; set; } = MaxBattleTokens;

    [Field(25, IsVarInt = true)] public int PlusBattleTokensS { get; set; } = PlusBattleTokens;

    [Field(26, IsVarInt = true)] public int Unk180 { get; set; }

    [Field(27, IsVarInt = true)] public int BigBoxCostSp { get; set; } = 10;

    [Field(28, IsVarInt = true)] public int Unk188 { get; set; }

    [Field(29)] public bool ForceTicketsEventEnable { get; set; }

    [Field(30)] public bool Unk193 { get; set; }

    [Field(31)] public bool Unk194 { get; set; }

    [Field(32, IsVarInt = true)] public int Unk196 { get; set; } = 50;

    [Field(33, IsVarInt = true)] public int Unk200 { get; set; } = 604800;

    [Field(34)] public bool BoxesAreAvailableInShop { get; set; } = true;

    [Field(35)] public List<ReleaseEntry> ReleaseEntries { get; set; } = [];

    // Key values:
    // 1 = lobby theme,
    // 14 = double token event.
    [Field(36)] public List<IntValueEntry> IntValues { get; set; } = [];
}