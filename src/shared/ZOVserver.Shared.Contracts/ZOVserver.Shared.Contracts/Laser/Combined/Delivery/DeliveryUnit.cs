using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Delivery;

[LaserSerializable]
public partial class DeliveryUnit : LaserContract
{
    [Field(0, IsVarInt = true)] public int Type { get; set; }

    [Field(1)] public List<GatchaDrop> GatchaDrops { get; set; } = [];
}