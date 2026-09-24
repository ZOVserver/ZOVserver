using ZOVserver.Shared.TitanRemnants.PAssets.Proto;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Input;

public class ClientInput
{
    public uint Index { get; private set; }

    public uint Type { get; private set; }

    public int X { get; private set; }
    public int Y { get; private set; }

    public bool AutoAttack { get; private set; }
    public int AutoAttackTarget { get; private set; }

    public void Decode(ref BitStream bitStream)
    {
        Index = bitStream.ReadPositiveIntMax32767();

        Type = bitStream.ReadPositiveIntMax7();

        X = bitStream.ReadIntMax32767();
        Y = bitStream.ReadIntMax32767();

        AutoAttack = bitStream.ReadBoolean();

        if (!AutoAttack)
            return;

        if (!bitStream.ReadBoolean())
            return;

        var v10 = bitStream.ReadPositiveIntMax16383();
        AutoAttackTarget = GlobalId.CreateGlobalId(1, (int)v10);
    }
}