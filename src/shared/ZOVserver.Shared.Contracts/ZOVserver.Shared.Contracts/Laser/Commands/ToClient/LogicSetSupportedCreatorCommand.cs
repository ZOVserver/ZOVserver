using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Entries;

namespace ZOVserver.Shared.Contracts.Laser.Commands.ToClient;

[LaserSerializable]
public partial class LogicSetSupportedCreatorCommand : LogicServerCommand
{
    [Field(0, PresenceBool = true, BaseMethodAfter = true)]
    public SupportedCreatorEntry? Code { get; set; }

    public override int GetCommandType()
    {
        return 215;
    }
}