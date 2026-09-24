using MessagePack;

namespace ZOVserver.Services.Game.PlayerSessionService.States;

[MessagePackObject]
public class PlayerSessionState
{
    [Key(0)] public int GameState { get; set; }
    [Key(1)] public Guid SessionId { get; set; }
    [Key(2)] public long AccountId { get; set; }
    [Key(3)] public DateTime LastKeepAliveReceivedTime { get; set; }

    [Key(4)] public Dictionary<string, int> ServersInfo { get; set; } = new();
    [Key(5)] public Dictionary<string, int> ClientsInfo { get; set; } = new();

    [Key(6)] public DateTime AccountCreatedTime { get; set; }
    [Key(7)] public DateTime AccountRemovedTime { get; set; }

    [Key(8)] public ulong PlayTimeSeconds { get; set; }
    [Key(9)] public int SessionsCount { get; set; }
    [Key(10)] public DateTimeOffset LastActiveTime { get; set; }

    [Key(11)] public uint ConnectionsCount { get; set; }

    [Key(12)] public uint DisconnectionsCount { get; set; }

    [Key(13)] public Dictionary<string, int> DevicesInfo { get; set; } = new();

    [Key(14)] public int PlayerLocalizationGlobalId { get; set; } = 1_000_000;
    [Key(15)] public string PlayerDeviceLanguage { get; set; } = "EN";

    [Key(16)] public DateTime LastLoginReceivedTime { get; set; }

    [Key(17)] public bool IsAccountBanned { get; set; }
    [Key(18)] public bool IsAccountLocked { get; set; }

    [Key(19)] public DateTime AccountBanEndTime { get; set; }
    [Key(20)] public string BanReason { get; set; } = string.Empty;

    [Key(21)] public string AccountUnlockCode { get; set; } = string.Empty;

    [Key(22)] public byte[]? HashedPassToken { get; set; }
}