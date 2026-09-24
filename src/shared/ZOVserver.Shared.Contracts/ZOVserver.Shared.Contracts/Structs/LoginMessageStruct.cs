using Orleans;

namespace ZOVserver.Shared.Contracts.Structs;

[Immutable]
[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Structs.LoginMessageStruct")]
public struct LoginMessageStruct
{
    [Id(0)] public long AccountId { get; set; }

    [Id(1)] public string PassToken { get; set; }

    [Id(2)] public int ClientMajor { get; set; }

    [Id(3)] public int ClientMinor { get; set; }

    [Id(4)] public int ClientBuild { get; set; }

    [Id(5)] public string ResourceSha { get; set; }

    [Id(6)] public string Device { get; set; }

    [Id(7)] public int PreferredLanguage { get; set; }

    [Id(8)] public string PreferredDeviceLanguage { get; set; }

    [Id(9)] public string OsVersion { get; set; }

    [Id(10)] public bool IsAndroid { get; set; }

    [Id(12)] public string AndroidId { get; set; }
}