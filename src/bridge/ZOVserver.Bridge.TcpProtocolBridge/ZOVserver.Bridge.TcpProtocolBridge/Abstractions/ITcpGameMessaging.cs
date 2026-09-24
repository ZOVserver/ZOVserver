using ZOVserver.Shared.Contracts.Laser.Messages;

namespace ZOVserver.Bridge.TcpProtocolBridge.Abstractions;

public interface ITcpGameMessaging : IAsyncDisposable
{
    public Task ReceiveBufferAsync(byte[] buffer);
    public Task SendAsync(PiranhaMessage piranhaMessage);
    public Task<int> EncryptAndWriteAsync(int type, int version, byte[]? payload);
}