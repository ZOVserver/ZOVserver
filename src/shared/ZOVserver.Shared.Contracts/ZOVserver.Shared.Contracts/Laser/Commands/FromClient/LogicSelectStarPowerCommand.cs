using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Commands.FromClient;

[LaserSerializable]
public partial class LogicSelectStarPowerCommand : LogicCommand
{
    [Field(0, BaseMethodBefore = true)] public int CardGlobalId { get; set; }

    public override int GetCommandType()
    {
        return 529;
    }
}