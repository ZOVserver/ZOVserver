using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class TeamSetLocationMessage : PiranhaMessage
{
    [Field(0, AsDataRef = true)] public int LocationGlobalId { get; set; }

    public override int GetMessageType()
    {
        return 14363;
    }

    public override int GetServiceNodeType()
    {
        return 23;
    }
}