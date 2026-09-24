using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Commands;

[LaserSerializable]
public partial class LogicCommand : LaserContract
{
    [Field(0)] public int Unk10000VInt { get; set; } = 1;

    [Field(1)] public int Unk20000VInt { get; set; } = 1;

    [Field(2)] public int Unk30000VInt { get; set; }

    [Field(3)] public int Unk40000VInt { get; set; }

    public virtual int GetCommandType()
    {
        return -1;
    }
}