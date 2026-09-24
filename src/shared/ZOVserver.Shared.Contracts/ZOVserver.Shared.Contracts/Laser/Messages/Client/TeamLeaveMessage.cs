namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

public class TeamLeaveMessage : PiranhaMessage
{
    public override int GetMessageType()
    {
        return 14353;
    }

    public override int GetServiceNodeType()
    {
        return 23;
    }
}