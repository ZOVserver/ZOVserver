using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class TeamChangeMemberSettingsMessage : PiranhaMessage
{
    [Field(0, AsDataRef = true)] public int StarPowerId { get; set; }
    [Field(1, AsDataRef = true)] public int SkinId { get; set; }

    public override int GetMessageType()
    {
        return 14354;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}