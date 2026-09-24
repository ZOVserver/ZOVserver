using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Home;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class OwnHomeDataMessage : PiranhaMessage
{
    [Field(0, UseCustomContract = true)] public LogicClientHome? Home { get; set; }

    [Field(1, UseCustomContract = true)] public LogicClientAvatar? Avatar { get; set; }

    [Field(2, IsVarInt = true)] public DateTime UnkDateTime { get; set; } = DateTime.UtcNow;

    public override int GetMessageType()
    {
        return 24101;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}