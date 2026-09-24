using System.Net;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using NLog;
using ZOVserver.Bridge.TcpProtocolBridge.Abstractions;
using ZOVserver.Bridge.TcpProtocolBridge.Sessions;

namespace ZOVserver.Bridge.TcpProtocolBridge.Networking;

internal class DotNettyTcpSession : SimpleChannelInboundHandler<IByteBuffer>
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private LaserHighLevelTcpSession? _tcpLaserHighLevelSession;

    private ILaserLLevelTcpSession? _tcpLaserLowLevelSession;

    public override void ChannelActive(IChannelHandlerContext context)
    {
        try
        {
            if (context.Channel.RemoteAddress is IPEndPoint ipEndPoint)
            {
                var clientIp = ipEndPoint.Address.ToString();

                var serverIp = "0.0.0.0";
                var serverPort = 0;

                if (clientIp.StartsWith("::ffff:"))
                    clientIp = clientIp[7..];

                if (context.Channel.LocalAddress is IPEndPoint localIpEndPoint)
                {
                    serverIp = localIpEndPoint.Address.ToString();
                    serverPort = localIpEndPoint.Port;
                }

                _tcpLaserLowLevelSession = new LaserLowLevelTcpSession(context)
                {
                    ClientId = Guid.NewGuid(),
                    ClientIp = clientIp, ClientPort = ipEndPoint.Port,
                    ServerIp = serverIp, ServerPort = serverPort
                };
            }

            if (_tcpLaserLowLevelSession == null)
            {
                if (context.Channel.Active)
                    context.CloseAsync();

                return;
            }

            _tcpLaserHighLevelSession = new LaserHighLevelTcpSession { LowLevelSession = _tcpLaserLowLevelSession! };

            _tcpLaserHighLevelSession.OnConnectedAsync();
        }
        catch (Exception exception)
        {
            Logger.Error(exception.ToString());

            if (context.Channel.Active)
                _ = context.CloseAsync();
        }
    }

    public override async void ChannelInactive(IChannelHandlerContext context)
    {
        try
        {
            if (_tcpLaserHighLevelSession == null) return;

            await _tcpLaserHighLevelSession.OnDisconnectedAsync();
            await _tcpLaserHighLevelSession.DisposeAsync();

            _tcpLaserHighLevelSession = null;
            _tcpLaserLowLevelSession = null;
        }
        catch (Exception exception)
        {
            Logger.Error(exception.ToString());

            if (context.Channel.Active)
                _ = context.CloseAsync();
        }
    }

    protected override async void ChannelRead0(IChannelHandlerContext context, IByteBuffer msg)
    {
        try
        {
            if (msg.ReadableBytes > 16384)
            {
                if (context.Channel.Active)
                    await context.CloseAsync();

                return;
            }

            var receivedData = GC.AllocateArray<byte>(msg.ReadableBytes);
            msg.ReadBytes(receivedData);

            if (_tcpLaserHighLevelSession == null)
            {
                if (context.Channel.Active)
                    await context.CloseAsync();

                return;
            }

            await _tcpLaserHighLevelSession.OnReceiveBufferAsync(receivedData);
        }
        catch (Exception exception)
        {
            Logger.Error(exception.ToString());

            if (context.Channel.Active)
                _ = context.CloseAsync();
        }
    }

    public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
    {
        Logger.Debug(exception.ToString());

        if (context.Channel.Active)
            _ = context.CloseAsync();
    }
}