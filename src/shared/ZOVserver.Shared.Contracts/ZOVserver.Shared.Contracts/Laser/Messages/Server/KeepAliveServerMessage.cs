namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

public class KeepAliveServerMessage : PiranhaMessage
{
    public override int GetMessageType()
    {
        return 20108;
    }

    public override int GetServiceNodeType()
    {
        return 1;
    }
}