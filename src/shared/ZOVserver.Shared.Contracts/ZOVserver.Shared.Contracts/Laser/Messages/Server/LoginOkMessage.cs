using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class LoginOkMessage : PiranhaMessage
{
    [Field(0)] public long AccountId { get; set; }

    [Field(1)] public long HomeId { get; set; }

    [Field(2)] public string? PassToken { get; set; }

    [Field(3)] public string? FacebookId { get; set; }

    [Field(4)] public string? GamecenterId { get; set; }

    [Field(5)] public int ServerMajorVersion { get; set; }

    [Field(6)] public int ContentVersion { get; set; }

    [Field(7)] public int ServerBuild { get; set; }

    [Field(8)] public string? ServerEnvironment { get; set; }

    [Field(9)] public int SessionCount { get; set; }

    [Field(10)] public int PlayTimeSeconds { get; set; }

    [Field(11)] public int DaysSinceStartedPlaying { get; set; }

    [Field(12)] public string? FacebookAppId { get; set; }

    [Field(13)] public string? ServerTime { get; set; }

    [Field(14)] public string? AccountCreatedDate { get; set; }

    [Field(15)] public int StartupCooldownSeconds { get; set; }

    [Field(16)] public string? GoogleServiceId { get; set; }

    [Field(17)] public string? LoginCountry { get; set; }

    [Field(18)] public string? KunlunId { get; set; }

    [Field(19)] public int Tier { get; set; }

    [Field(20)] public string? TencentId { get; set; }

    [Field(21, CountIsI32 = true)] public List<string> GameAssetsUrls { get; set; } = [];

    [Field(22, CountIsI32 = true)] public List<string> EventAssetsUrls { get; set; } = [];

    [Field(23, IsVarInt = true)] public int SecondsUntilAccountDeletion { get; set; }

    [Field(24, Compressed = true)] public string SupercellIdToken { get; set; } = string.Empty;

    [Field(25)] public bool IsSupercellIdLogoutAllDevicesAllowed { get; set; }

    [Field(26)] public bool IsSupercellIdEligible { get; set; }

    [Field(27)] public string? LineId { get; set; }

    public override int GetMessageType()
    {
        return 20104;
    }

    public override int GetServiceNodeType()
    {
        return 1;
    }
}