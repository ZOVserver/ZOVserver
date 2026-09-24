namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

public class CancelMatchmakingMessage : PiranhaMessage
{
    public override int GetMessageType()
    {
        return 14106;
    }

    public override int GetServiceNodeType()
    {
        return 7;
    }
}