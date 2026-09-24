namespace ZOVserver.Shared.Contracts.Laser.Commands.FromClient;

public class LogicPurchaseDoubleCoinsCommand : LogicCommand
{
    public override int GetCommandType()
    {
        return 509;
    }
}