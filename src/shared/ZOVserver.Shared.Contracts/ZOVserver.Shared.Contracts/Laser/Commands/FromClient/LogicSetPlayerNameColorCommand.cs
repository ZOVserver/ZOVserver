using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Commands.FromClient;

[LaserSerializable]
public partial class LogicSetPlayerNameColorCommand : LogicCommand
{
    [Field(0, BaseMethodBefore = true)] public int ColorGlobalId { get; set; }

    public override int GetCommandType()
    {
        return 527;
    }
}