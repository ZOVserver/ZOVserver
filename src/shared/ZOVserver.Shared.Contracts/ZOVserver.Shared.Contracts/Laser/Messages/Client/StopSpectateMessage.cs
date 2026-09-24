namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

public class StopSpectateMessage : PiranhaMessage
{
    public override int GetMessageType()
    {
        return 14107;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}