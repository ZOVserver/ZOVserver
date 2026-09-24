namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

public class MatchMakingCancelledMessage : PiranhaMessage
{
    public override int GetMessageType()
    {
        return 20406;
    }

    public override int GetServiceNodeType()
    {
        return 4;
    }
}