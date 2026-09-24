using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA;

namespace ZOVserver.Shared.Contracts.Laser.Commands.ToClient;

[LaserSerializable]
public partial class LogicDayChangedCommand : LogicServerCommand
{
    [Field(0, PresenceBool = true, BaseMethodAfter = true)]
    public LogicConfData? LogicConfData { get; set; }

    public override int GetCommandType()
    {
        return 204;
    }
}