using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;

[MessagePackObject]
[LaserSerializable]
public partial class EventSlot : LaserContract
{
    [Key(0)] [Field(0, IsVarInt = true)] public int Slot { get; set; }

    [Key(1)] public bool Unlocked { get; set; }
}