using ZOVserver.Shared.Contracts.Laser.Messages;

namespace ZOVserver.Shared.Abstractions;

public interface IMessageManager
{
    public static abstract IReadOnlyList<(int messageType, string messageName)> GetAllImplementedMessages();

    public Task<int> ReceiveMessageAsync(PiranhaMessage piranhaMessage);

    public Task<bool> SendMessagesAsync(params PiranhaMessage[] piranhaMessage);
    public Task<bool> SendMessagesAndDisconnectAsync(params PiranhaMessage[] piranhaMessage);

    public Task TickAsync();

    public ValueTask GoodbyeAsync();
}