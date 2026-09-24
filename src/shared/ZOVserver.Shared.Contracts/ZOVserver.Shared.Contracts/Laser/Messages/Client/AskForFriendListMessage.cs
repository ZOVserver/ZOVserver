namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

public class AskForFriendListMessage : PiranhaMessage
{
    public override int GetMessageType()
    {
        return 10504;
    }

    public override int GetServiceNodeType()
    {
        return 26;
    }
}