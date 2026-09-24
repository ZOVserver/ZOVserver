using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Commands.ToClient;

[LaserSerializable]
public partial class LogicInviteBlockingChangedCommand : LogicServerCommand
{
    [Field(0, BaseMethodAfter = true)] public bool State { get; set; }

    public override int GetCommandType()
    {
        return 213;
    }
}