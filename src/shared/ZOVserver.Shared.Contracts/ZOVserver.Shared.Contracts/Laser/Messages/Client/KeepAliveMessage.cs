namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

public class KeepAliveMessage : PiranhaMessage
{
    public override int GetMessageType()
    {
        return 10108;
    }

    public override int GetServiceNodeType()
    {
        return 1;
    }
}