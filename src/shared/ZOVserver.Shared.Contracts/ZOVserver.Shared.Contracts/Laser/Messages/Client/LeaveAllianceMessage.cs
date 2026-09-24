namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

public class LeaveAllianceMessage : PiranhaMessage
{
    public override int GetMessageType()
    {
        return 14308;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}