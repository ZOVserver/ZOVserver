using YamlDotNet.Serialization;

// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable InconsistentNaming
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable ClassNeverInstantiated.Local

namespace ZOVserver.Services.Game.TeamService.Settings;

public static class TeamSettings
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
    public int MaxMessagesInChat { get; set; }
}