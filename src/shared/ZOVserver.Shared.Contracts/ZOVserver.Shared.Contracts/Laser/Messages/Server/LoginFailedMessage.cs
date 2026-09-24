using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class LoginFailedMessage : PiranhaMessage
{
    [Field(0)] public int ErrorCode { get; set; }

    [Field(1)] public string RemoveResourceFingerprintData { get; set; } = string.Empty;

    [Field(2)] public string RedirectDomain { get; set; } = string.Empty;

    [Field(3)] public string ContentUrl { get; set; } = string.Empty;

    [Field(4)] public string UpdateUrl { get; set; } = string.Empty;

    [Field(5)] public string Reason { get; set; } = string.Empty;

    [Field(6)] public int SecondsUntilMaintenanceEnd { get; set; }

    [Field(7)] public bool ShowContactSupportForBan { get; set; }

    [Field(8)] public byte[] CompressedResourceFingerprintData { get; set; } = [];

    [Field(9, CountIsI32 = true)] public List<string> ContentUrlList { get; set; } = [];

    [Field(10)] public int KunlunAppStore { get; set; }

    [Field(11)] public int MaintenanceType { get; set; }

    [Field(12)] public string HelpshiftFaqId { get; set; } = string.Empty;

    [Field(13)] public int Tier { get; set; }

    [Field(14)] public bool UnkBool113 { get; set; }

    [Field(15)] public bool UnkBool128 { get; set; }

    public override int GetMessageType()
    {
        return 20103;
    }

    public override int GetServiceNodeType()
    {
        return 1;
    }
}