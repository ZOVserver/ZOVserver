namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

public class AskForAllianceStreamMessage : PiranhaMessage
{
    public override int GetMessageType()
    {
        return 14304;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}