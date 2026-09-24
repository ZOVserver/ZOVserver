using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Commands.FromClient;

[LaserSerializable]
public partial class LogicSelectSkinCommand : LogicCommand
{
    [Field(0, BaseMethodBefore = true)] public int SkinGlobalId { get; set; }

    public override int GetCommandType()
    {
        return 506;
    }
}