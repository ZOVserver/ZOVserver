namespace ZOVserver.Bridge.TcpProtocolBridge.Abstractions;

public interface ILaserHLevelTcpSession : IAsyncDisposable
{
    public ILaserLLevelTcpSession LowLevelSession { get; init; }

    public Task OnConnectedAsync();
    public Task OnDisconnectedAsync();

    public Task OnReceiveBufferAsync(byte[] buffer);
}