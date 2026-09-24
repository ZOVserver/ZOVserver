using ZOVserver.Shared.Contracts.Laser.Combined.Stream.Inheritors;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Stream.Factory;

public static class StreamFactory
{
    public static StreamEntry CreateStreamByType(int type)
    {
        return type switch
        {
            2 => new ChatStreamEntry(),
            3 => new JoinRequestAllianceStreamEntry(),
            4 => new AllianceEventStreamEntry(),
            6 => new MessageDataStreamEntry(),
            8 => new QuickChatStreamEntry(),
            _ => throw new ArgumentException($"Unknown stream type: {type}")
        };
    }
}