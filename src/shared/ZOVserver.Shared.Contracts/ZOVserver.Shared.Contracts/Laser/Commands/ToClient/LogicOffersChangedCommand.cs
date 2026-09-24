using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;

namespace ZOVserver.Shared.Contracts.Laser.Commands.ToClient;

[LaserSerializable]
public partial class LogicOffersChangedCommand : LogicServerCommand
{
    [Field(0, BaseMethodAfter = true)] public List<LogicOfferBundles> OfferBundles { get; set; } = [];

    public override int GetCommandType()
    {
        return 211;
    }
}