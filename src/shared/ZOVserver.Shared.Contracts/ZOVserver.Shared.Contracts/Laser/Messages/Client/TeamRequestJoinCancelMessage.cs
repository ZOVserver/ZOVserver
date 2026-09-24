namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

public class TeamRequestJoinCancelMessage : PiranhaMessage
{
    public override int GetMessageType()
    {
        return 14880;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}