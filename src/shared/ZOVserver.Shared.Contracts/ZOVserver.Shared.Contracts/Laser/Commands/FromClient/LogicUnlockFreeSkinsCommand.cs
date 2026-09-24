namespace ZOVserver.Shared.Contracts.Laser.Commands.FromClient;

public class LogicUnlockFreeSkinsCommand : LogicCommand
{
    public override int GetCommandType()
    {
        return 526;
    }
}