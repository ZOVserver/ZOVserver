using YamlDotNet.Serialization;

// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable InconsistentNaming
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable ClassNeverInstantiated.Local

namespace ZOVserver.Services.Game.HomeService.Settings;

public static class HomeSettings
{
    private static GameConfig _config = null!;

    public static void Load(string yamlText)
    {
        _config = new DeserializerBuilder().Build().Deserialize<GameConfig>(yamlText);
    }

    public static GameConfig GetConfig()
    {
        return _config;
    }
}

public class GameConfig
{
    public Resources Resources { get; set; }
    public Boxes Boxes { get; set; }
    public Brawlers Brawlers { get; set; }
    public Progress Progress { get; set; }
    public TimeSettings TimeSettings { get; set; }
    public Decorations Decorations { get; set; }

    public GachaSystem GachaSystem { get; set; }
}

public class Resources
{
    public int StartingGold { get; set; }
    public int StartingGems { get; set; }
    public int StartingTickets { get; set; }
    public int StartingStarPoints { get; set; }
}

public class Boxes
{
    public int StartingMiniBoxesCount { get; set; }
    public int StartingBigBoxesCount { get; set; }
}

public class Brawlers
{
    public int[] StartingHeroGIds { get; set; }
}

public class Progress
{
    public int DefaultTutorialState { get; set; }
    public int StartingTrophies { get; set; }
    public int StartingExperience { get; set; }
}

public class TimeSettings
{
    public int NicknameChangeCooldownHours { get; set; }
}

public class Decorations
{
    public int DefaultLobbyThemeGid { get; set; }
    public bool ShowBattleGameHints { get; set; }
}

public class GachaSystem
{
    public int NewAccountPityCounterForRareBrawlers { get; set; }
    public int NewAccountPityCounterForSuperRareBrawlers { get; set; }

    public int DefaultPityCounterForRareBrawlers { get; set; }
    public int DefaultPityCounterForSuperRareBrawlers { get; set; }
    public int DefaultPityCounterForEpicBrawlers { get; set; }
    public int DefaultPityCounterForMythicBrawlers { get; set; }
    public int DefaultPityCounterForLegendaryBrawlers { get; set; }
    public int DefaultPityCounterForStarPower { get; set; }

    public float DefaultRareBrawlerChance { get; set; }
    public float DefaultSuperRareBrawlerChance { get; set; }
    public float DefaultEpicBrawlerChance { get; set; }
    public float DefaultMythicBrawlerChance { get; set; }
    public float DefaultLegendaryBrawlerChance { get; set; }
    public float DefaultStarPowerChance { get; set; }
}