namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

public class TeamTogglePractiseMessage : PiranhaMessage
{
    public override int GetMessageType()
    {
        return 14356;
    }

    public override int GetServiceNodeType()
    {
        return 23;
    }
}