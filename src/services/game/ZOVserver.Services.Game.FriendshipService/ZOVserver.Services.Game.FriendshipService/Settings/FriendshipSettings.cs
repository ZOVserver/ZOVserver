using YamlDotNet.Serialization;

// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable InconsistentNaming
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable ClassNeverInstantiated.Local

namespace ZOVserver.Services.Game.FriendshipService.Settings;

public static class FriendshipSettings
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
    public int MaxFriendsCount { get; set; }
    public int MaxSuggestionsCount { get; set; }
    public int MaxFriendRequestsCount { get; set; }
}