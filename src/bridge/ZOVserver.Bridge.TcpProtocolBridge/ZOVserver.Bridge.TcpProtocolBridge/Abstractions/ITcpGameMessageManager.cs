using ZOVserver.Shared.Contracts.Laser.Messages;

namespace ZOVserver.Bridge.TcpProtocolBridge.Abstractions;

public interface ITcpGameMessageManager
{
    public Task<int> ReceiveMessageAsync(PiranhaMessage piranhaMessage);
    public Task<int> ReceiveMaybeImplementedMessagesByGrains(List<PiranhaMessage> piranhaMessage);

    public Task TickAsync();
    public ValueTask GoodbyeAsync();
}