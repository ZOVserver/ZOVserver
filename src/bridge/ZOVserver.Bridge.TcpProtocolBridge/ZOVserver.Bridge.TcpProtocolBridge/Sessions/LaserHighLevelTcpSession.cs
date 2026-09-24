using NLog;
using ZOVserver.Bridge.TcpProtocolBridge.Abstractions;
using ZOVserver.Bridge.TcpProtocolBridge.Messaging;
using Timer = System.Timers.Timer;

namespace ZOVserver.Bridge.TcpProtocolBridge.Sessions;

public class LaserHighLevelTcpSession : ILaserHLevelTcpSession
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    private TcpGameMessageManager? _gameMessageManager;
    private TcpGameMessaging? _gameMessaging;

    private Timer? _tickTimer;

    public Guid SessionId { get; } = Guid.NewGuid();

    public bool IsDisposed { get; set; }
    public required ILaserLLevelTcpSession LowLevelSession { get; init; }

    public Task OnConnectedAsync()
    {
        _gameMessaging = new TcpGameMessaging(this);
        _gameMessageManager = new TcpGameMessageManager(this);

        _tickTimer = new Timer(5000);
        _tickTimer.Elapsed += async (_, _) => await _gameMessageManager.TickAsync();
        _tickTimer.Start();

        Logger.Info($"New user connected! IP: {LowLevelSession.ClientIp}; Port: {LowLevelSession.ClientPort}");
        return Task.CompletedTask;
    }

    public async Task OnDisconnectedAsync()
    {
        _tickTimer?.Stop();

        if (_gameMessageManager != null)
            await _gameMessageManager.GoodbyeAsync();

        Logger.Info($"User disconnected! IP: {LowLevelSession.ClientIp}; Port: {LowLevelSession.ClientPort}");
    }

    public async Task OnReceiveBufferAsync(byte[] buffer)
    {
        if (IsDisposed) return;

        await _processingSemaphore.WaitAsync();

        try
        {
            if (_gameMessaging == null)
            {
                await LowLevelSession.DisconnectAsync();
                return;
            }

            await _gameMessaging.ReceiveBufferAsync(buffer);
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (IsDisposed) return;
        IsDisposed = true;

        _tickTimer?.Dispose();
        _tickTimer = null;

        if (_gameMessaging != null)
            await _gameMessaging.DisposeAsync();
        _gameMessaging = null;

        _processingSemaphore.Dispose();
    }

    public ITcpGameMessaging? GetGameMessaging()
    {
        return _gameMessaging;
    }

    public ITcpGameMessageManager? GetGameMessageManager()
    {
        return _gameMessageManager;
    }
}