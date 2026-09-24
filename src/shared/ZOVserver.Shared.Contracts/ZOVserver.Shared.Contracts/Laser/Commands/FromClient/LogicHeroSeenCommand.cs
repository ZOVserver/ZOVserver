using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Commands.FromClient;

[LaserSerializable]
public partial class LogicHeroSeenCommand : LogicCommand
{
    [Field(0, BaseMethodBefore = true)] public int HeroGlobalId { get; set; }

    [Field(1)] public int State { get; set; }

    public override int GetCommandType()
    {
        return 522;
    }
}