using MessagePack;
using Orleans;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Shared.Contracts.Laser.Machine;

[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Laser.Machine.LaserContract")]
public abstract class LaserContract
{
    [IgnoreMember] [Id(0)] public int Capacity { get; set; } = 256;

    public virtual void Encode(ByteStream stream)
    {
    }

    public virtual void Decode(ByteStream stream)
    {
    }

    public virtual void CustomEncode(ByteStream stream)
    {
    }

    public virtual void CustomDecode(ByteStream stream)
    {
    }
}