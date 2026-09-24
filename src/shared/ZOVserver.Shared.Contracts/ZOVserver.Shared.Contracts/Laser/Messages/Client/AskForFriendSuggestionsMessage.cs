namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

public class AskForFriendSuggestionsMessage : PiranhaMessage
{
    public override int GetMessageType()
    {
        return 10599;
    }

    public override int GetServiceNodeType()
    {
        return 26;
    }
}