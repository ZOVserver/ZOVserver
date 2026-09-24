using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using ZOVserver.Bridge.TcpProtocolBridge.Abstractions;

namespace ZOVserver.Bridge.TcpProtocolBridge.Sessions;

public class LaserLowLevelTcpSession(IChannelHandlerContext context) : ILaserLLevelTcpSession
{
    public bool IsActive { get; } = context.Channel.Active;

    public required Guid ClientId { get; init; }

    public required string ClientIp { get; init; }
    public required int ClientPort { get; init; }

    public required string ServerIp { get; init; }
    public required int ServerPort { get; init; }

    public async Task SendBufferAsync(byte[] buffer)
    {
        if (IsActive && context.Channel.IsWritable)
        {
            var byteBuffer = Unpooled.WrappedBuffer(buffer);
            await context.Channel.WriteAndFlushAsync(byteBuffer);
        }
    }

    public async Task DisconnectAsync()
    {
        if (IsActive)
            await context.CloseAsync();
    }
}