using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Commands.FromClient;

[LaserSerializable]
public partial class LogicLevelUpCommand : LogicCommand
{
    [Field(0, BaseMethodBefore = true)] public int BrawlerGlobalId { get; set; }

    public override int GetCommandType()
    {
        return 520;
    }
}