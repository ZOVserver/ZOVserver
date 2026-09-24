namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

public class DynamicClientMessage(int type, int node) : PiranhaMessage
{
    public override int GetMessageType()
    {
        return type;
    }

    public override int GetServiceNodeType()
    {
        return node;
    }
}