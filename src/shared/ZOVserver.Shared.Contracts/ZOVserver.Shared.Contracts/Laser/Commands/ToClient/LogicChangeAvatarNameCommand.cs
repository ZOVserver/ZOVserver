using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Commands.ToClient;

[LaserSerializable]
public partial class LogicChangeAvatarNameCommand : LogicServerCommand
{
    [Field(0)] public string? NewName { get; set; }

    [Field(1, IsVarInt = true, BaseMethodAfter = true)]
    public int ChangeNamePrice { get; set; }

    public override int GetCommandType()
    {
        return 201;
    }
}