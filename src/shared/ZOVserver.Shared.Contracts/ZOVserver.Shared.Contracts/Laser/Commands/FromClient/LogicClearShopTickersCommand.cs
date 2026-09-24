using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Commands.FromClient;

[LaserSerializable]
public partial class LogicClearShopTickersCommand : LogicCommand
{
    [Field(0, BaseMethodBefore = true)] public int Unk1VInt { get; set; }

    [Field(1)] public int Unk2VInt { get; set; }

    [Field(2)] public int Unk3VInt { get; set; }

    [Field(3)] public int Unk4VInt { get; set; }

    public override int GetCommandType()
    {
        return 515;
    }
}