using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Commands.FromClient;

[LaserSerializable]
public partial class LogicPurchaseOfferCommand : LogicCommand
{
    [Field(0, IsVarInt = true, BaseMethodBefore = true)]
    public int OfferIndex { get; set; }

    [Field(1)] public int SelectedDataGlobalId { get; set; }

    public override int GetCommandType()
    {
        return 519;
    }
}