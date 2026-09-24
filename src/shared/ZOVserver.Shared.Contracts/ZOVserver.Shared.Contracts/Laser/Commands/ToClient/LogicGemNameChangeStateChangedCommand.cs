using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Commands.ToClient;

[LaserSerializable]
public partial class LogicGemNameChangeStateChangedCommand : LogicServerCommand
{
    [Field(0, IsVarInt = true)] public int NextNameChangePrice { get; set; }

    [Field(1, IsVarInt = true, BaseMethodAfter = true)]
    public int NextNameChangeSeconds { get; set; }

    public override int GetCommandType()
    {
        return 214;
    }
}