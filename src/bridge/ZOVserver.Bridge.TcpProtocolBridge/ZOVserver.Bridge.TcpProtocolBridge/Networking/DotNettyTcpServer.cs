using System.Net;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using NLog;
using Timer = System.Timers.Timer;

namespace ZOVserver.Bridge.TcpProtocolBridge.Networking;

public static class DotNettyTcpServer
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static bool DoesAcceptConnections { get; set; }

    public static async Task Start(ushort port, int maxConnsPerMin)
    {
        var state = new ServerState();

        try
        {
            await StartServer(port, maxConnsPerMin, state);
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Critical failure during server startup on port {Port}!!!", port);
            throw;
        }
    }

    private static async Task StartServer(ushort port, int maxConnsPerMin, ServerState state)
    {
        Logger.Info("Starting TCP server on port {Port}...", port);

        state.BossGroup ??= new MultithreadEventLoopGroup(3);
        state.WorkerGroup ??= new MultithreadEventLoopGroup(6);

        var bootstrap = new ServerBootstrap();

        bootstrap.Group(state.BossGroup, state.WorkerGroup);
        bootstrap.Channel<TcpServerSocketChannel>();
        bootstrap.Option(ChannelOption.SoBacklog, 16384);
        bootstrap.ChildOption(ChannelOption.TcpNodelay, true);

        bootstrap.ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
        {
            try
            {
                if (!DoesAcceptConnections)
                {
                    Logger.Trace("Server is not accepting connections.");
                    return;
                }

                if (Interlocked.CompareExchange(ref state.IsInCooldown, 0, 0) == 1)
                {
                    Logger.Trace("Rejecting connection during cooldown.");
                    channel.CloseAsync();
                    return;
                }

                var currentCount = Interlocked.Increment(ref state.ConnectionCount);
                Logger.Trace("New connection accepted. Total connections: {ConnectionCount}.", currentCount);

                if (currentCount > maxConnsPerMin)
                {
                    Logger.Warn("Connection limit exceeded ({ConnectionCount} > {Max}). Initiating cooldown.",
                        currentCount, maxConnsPerMin);
                    InitiateCooldown(port, maxConnsPerMin, state);
                    return;
                }

                channel.Pipeline.AddLast(new DotNettyTcpSession());
                Logger.Trace("Pipeline initialized for new connection!");
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error while initializing client connection!");
            }
        }));

        state.ConnectionRateTimer = new Timer(60_000) { AutoReset = true };
        state.ConnectionRateTimer.Elapsed += (_, _) =>
        {
            var count = Interlocked.Exchange(ref state.ConnectionCount, 0);
            Logger.Trace("Connection count reset. Previous count: {ConnectionCount}.", count);
        };
        state.ConnectionRateTimer.Start();

        state.ServerChannel = await bootstrap.BindAsync(IPAddress.Any, port);
        Logger.Info("TCP server successfully started and listening on port {Port} (MCPM={Mcpm})!", port,
            maxConnsPerMin);
    }

    private static void InitiateCooldown(int port, int maxConnsPerMin, ServerState state)
    {
        if (Interlocked.CompareExchange(ref state.IsInCooldown, 1, 0) != 0)
        {
            Logger.Warn("Cooldown already in progress!");
            return;
        }

        if (state.ServerChannel != null)
        {
            Logger.Warn("Initiating server cooldown on port {Port}!", port);
            state.ServerChannel.CloseAsync();
            Logger.Warn("Server on port {Port} stopped for cooldown!", port);
        }

        state.CooldownTimer = new Timer(150_000) { AutoReset = false };

        state.CooldownTimer.Elapsed += async (_, _) =>
        {
            state.CooldownTimer.Stop();
            Interlocked.Exchange(ref state.IsInCooldown, 0);

            Logger.Info("Cooldown completed for port {Port}. Restarting server...", port);
            await StartServer((ushort)port, maxConnsPerMin, state);
        };

        state.CooldownTimer.Start();
        Logger.Info("Cooldown timer started for port {Port} (duration: 150s).", port);
    }

    private class ServerState
    {
        public MultithreadEventLoopGroup? BossGroup;

        public int ConnectionCount;
        public Timer? ConnectionRateTimer;
        public Timer? CooldownTimer;
        public int IsInCooldown;
        public IChannel? ServerChannel;
        public MultithreadEventLoopGroup? WorkerGroup;
    }
}