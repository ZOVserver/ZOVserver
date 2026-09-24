using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Commands;

[LaserSerializable]
public abstract partial class LogicServerCommand : LogicCommand
{
    [Field(0, IsVarInt = true, BaseMethodAfter = true)]
    public int Id { get; set; }
}