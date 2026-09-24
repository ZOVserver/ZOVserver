namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

public class AskForJoinableAlliancesListMessage : PiranhaMessage
{
    public override int GetMessageType()
    {
        return 14303;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}