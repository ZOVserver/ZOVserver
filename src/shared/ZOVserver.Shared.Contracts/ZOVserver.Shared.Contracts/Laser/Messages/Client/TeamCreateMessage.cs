using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Team;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class TeamCreateMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public int LocationId { get; set; }
    [Field(1, IsVarInt = true)] public int EventSlot { get; set; }
    [Field(2, IsVarInt = true)] public int RoomType { get; set; }
    [Field(3)] public bool Unk4 { get; set; }
    [Field(4, PresenceBool = true)] public TeamCreateMessageSegment? Segment { get; set; }

    public override int GetMessageType()
    {
        return 14350;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}