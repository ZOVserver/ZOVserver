namespace ZOVserver.Bridge.TcpProtocolBridge.Abstractions;

public interface ILaserLLevelTcpSession
{
    public bool IsActive { get; }

    public Guid ClientId { get; init; }

    public string ClientIp { get; init; }
    public int ClientPort { get; init; }

    public string ServerIp { get; init; }
    public int ServerPort { get; init; }

    public Task SendBufferAsync(byte[] buffer);
    public Task DisconnectAsync();
}