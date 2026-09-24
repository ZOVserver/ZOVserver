using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Commands.FromClient;

[LaserSerializable]
public partial class LogicPurchaseHeroLvlUpMaterialCommand : LogicCommand
{
    [Field(0, IsVarInt = true, BaseMethodBefore = true)]
    public int PackIndex { get; set; }

    public override int GetCommandType()
    {
        return 521;
    }
}