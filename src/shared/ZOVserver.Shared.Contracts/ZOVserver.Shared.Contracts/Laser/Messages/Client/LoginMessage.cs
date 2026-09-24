using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class LoginMessage : PiranhaMessage
{
    [Field(0)] public long AccountId { get; set; }

    [Field(1)] public string PassToken { get; set; } = string.Empty;

    [Field(2)] public int ClientMajor { get; set; }

    [Field(3)] public int ClientMinor { get; set; }

    [Field(4)] public int ClientBuild { get; set; }

    [Field(5)] public string ResourceSha { get; set; } = string.Empty;

    [Field(6)] public string Unknown1 { get; set; } = string.Empty;

    [Field(7)] public string DeviceId { get; set; } = string.Empty;

    [Field(8)] public string Unknown2 { get; set; } = string.Empty;

    [Field(9)] public string Device { get; set; } = string.Empty;

    [Field(10, AsDataRef = true)] public int PreferredLanguage { get; set; }

    [Field(11)] public string PreferredDeviceLanguage { get; set; } = string.Empty;

    [Field(12)] public string DeviceUuid { get; set; } = string.Empty;

    [Field(13)] public string OsVersion { get; set; } = string.Empty;

    [Field(14)] public bool IsAndroid { get; set; }

    [Field(15)] public string Unknown3 { get; set; } = string.Empty;

    [Field(16)] public string AndroidId { get; set; } = string.Empty;

    [Field(17)] public string Unknown4 { get; set; } = string.Empty;

    [Field(18)] public bool IsAdvertisingEnabled { get; set; }

    [Field(19)] public string Unknown5 { get; set; } = string.Empty;

    [Field(20)] public int Unknown6 { get; set; }

    [Field(21, IsVarInt = true)] public int AppStore { get; set; }

    [Field(22)] public string Unknown7 { get; set; } = string.Empty;

    [Field(23)] public string Unknown8 { get; set; } = string.Empty;

    [Field(24)] public string AppVersion { get; set; } = string.Empty;

    [Field(25)] public string Unknown9 { get; set; } = string.Empty;

    [Field(26)] public string Unknown10 { get; set; } = string.Empty;

    [Field(27, IsVarInt = true)] public int TencentPlatform { get; set; }

    [Field(28)] public string Unknown11 { get; set; } = string.Empty;

    [Field(29)] public string Unknown12 { get; set; } = string.Empty;

    [Field(30)] public string Unknown13 { get; set; } = string.Empty;

    public bool CreatedInServer { get; set; }

    public override int GetMessageType()
    {
        return 10101;
    }

    public override int GetServiceNodeType()
    {
        return 1;
    }
}