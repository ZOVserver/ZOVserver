namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

public class GetBattleLogMessage : PiranhaMessage
{
    public override int GetMessageType()
    {
        return 14114;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}