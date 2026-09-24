using YamlDotNet.Serialization;

namespace ZOVserver.Services.Game.ContentCreatorRewardService.Settings;

public static class ContentCreatorSettings
{
    private static Config _config = null!;

    public static void Load(string yamlText)
    {
        _config = new DeserializerBuilder().Build().Deserialize<Config>(yamlText);
    }

    public static Config GetConfig()
    {
        return _config;
    }
}

public class Config
{
    public decimal MagicForLevel { get; set; }
    public int MinDiamondsForReward { get; set; }
}