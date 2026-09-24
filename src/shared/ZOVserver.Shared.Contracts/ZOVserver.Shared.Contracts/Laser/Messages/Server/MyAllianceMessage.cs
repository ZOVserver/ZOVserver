using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class MyAllianceMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public int OnlineMembers { get; set; }
    [Field(1, PresenceBool = true)] public MyAllianceObject? MyAllianceObject { get; set; }

    public override int GetMessageType()
    {
        return 24399;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}