using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class ChangeAllianceSettingsMessage : PiranhaMessage
{
    [Field(0)] public string AllianceDescription { get; set; } = string.Empty;
    [Field(1, AsDataRef = true)] public int AllianceBadgeData { get; set; }
    [Field(2, AsDataRef = true)] public int AllianceRegion { get; set; }
    [Field(3, IsVarInt = true)] public int AllianceType { get; set; }
    [Field(4, IsVarInt = true)] public int RequiredScore { get; set; }

    public override int GetMessageType()
    {
        return 14316;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}