using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using NLog;
using ZOVserver.Services.Game.BattleService.Game;
using ZOVserver.Shared.Contracts.Laser.Messages;
using ZOVserver.Shared.Contracts.Laser.Messages.Client;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Services.Game.BattleService.Network;

public sealed class UdpBattleServerInstance(int port)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Channel<UdpRawPacket> _channel = Channel.CreateBounded<UdpRawPacket>(
        new BoundedChannelOptions(ChannelCapacity) { FullMode = BoundedChannelFullMode.DropOldest });

    private readonly ConcurrentDictionary<SessionId, (IPEndPoint Ep, DateTime LastSeen)> _sessions = new();

#pragma warning disable S3887
    public readonly ConcurrentDictionary<SessionId, LogicBattleModeServer> Sessions = new();
#pragma warning restore S3887

    private long _packetCount;

    private Socket? _socket;

    public static string ServerIp { get; set; } = "";
    public int Port { get; set; } = port;

    public static int ChannelCapacity { get; set; } = 200_000;
    public static int SocketBufferSize { get; set; } = 16 * 1024 * 1024;
    public static int SessionTimeoutSeconds { get; set; } = 60;
    public static int WorkerCount { get; set; } = 3;

    public int ActiveSessionsCount => _sessions.Count;
    public long CurrentPps { get; private set; }

    public Task RunAsync(CancellationToken ct = default)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        _socket.ReceiveBufferSize = SocketBufferSize;
        _socket.Bind(new IPEndPoint(IPAddress.Any, Port));

        Logger.Info($"New UdpBattleServerInstance listening on port {ServerIp}:{Port}!");

        _ = RunMetricsAndCleanupAsync(ct);

        for (var i = 0; i < WorkerCount; i++)
            _ = ProcessQueueAsync(ct);

        StartReceiving();

        return Task.CompletedTask;
    }

    private void StartReceiving()
    {
        var receiveThread = new Thread(ReceiveLoop)
        {
            IsBackground = true
        };

        receiveThread.Start();
    }

    private void ReceiveLoop()
    {
        EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (_socket != null)
            try
            {
                var buffer = ArrayPool<byte>.Shared.Rent(1024);

                var receivedBytes = _socket.ReceiveFrom(buffer, ref remoteEndPoint);

                Interlocked.Increment(ref _packetCount);

                if (!_channel.Writer.TryWrite(new UdpRawPacket(buffer, receivedBytes, remoteEndPoint)))
                    ArrayPool<byte>.Shared.Return(buffer);
            }
            catch (SocketException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in ReceiveLoop");
            }
    }

    private async Task ProcessQueueAsync(CancellationToken ct)
    {
        await foreach (var packet in _channel.Reader.ReadAllAsync(ct))
            try
            {
                ReadOnlySpan<byte> bufferSpan = packet.Buffer.AsSpan(0, packet.Length);

                var low = BinaryPrimitives.ReadUInt64BigEndian(bufferSpan[..8]);
                var high = BinaryPrimitives.ReadUInt16BigEndian(bufferSpan[8..10]);

                var sessionId = new SessionId(low, high);

                _sessions[sessionId] = ((IPEndPoint)packet.Remote, DateTime.UtcNow);

                var encryptedPayload = bufferSpan[10..];
                HandlePacket(sessionId, encryptedPayload);
            }
            catch
            {
                // ignored.
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(packet.Buffer);
            }
    }

    private void HandlePacket(SessionId sid, ReadOnlySpan<byte> encryptedPayload)
    {
        if (!Sessions.TryGetValue(sid, out var battleMode))
            return;

        if (!battleMode.State.BCryptoStates.TryGetValue(sid, out var s))
            return;

        var payload = s.Crypto.D(encryptedPayload);

        if (payload == null)
            return;

        var bs = new ByteStream(payload);

        var messageType = bs.ReadVInt32();
        var messageLength = bs.ReadVInt32();
        var messagePayload = bs.ReadBytesWithoutLength(messageLength);

        if (messageType != 10555)
            return;

        var bitStream = new BitStream(messagePayload.ToArray());

        var input = new ClientInputMessage();
        input.Decode(ref bitStream);

        bitStream.Dispose();

        var inputsSpan = CollectionsMarshal.AsSpan(input.Inputs);

        foreach (var clientInput in inputsSpan)
            battleMode.AddClientInput(in sid, clientInput);
    }

    public void SendMessage(SessionId sid, PiranhaMessage piranhaMessage)
    {
        if (_socket == null)
            return;

        if (!_sessions.TryGetValue(sid, out var session))
            return;

        if (!Sessions.TryGetValue(sid, out var battleMode))
            return;

        if (!battleMode.State.BCryptoStates.TryGetValue(sid, out var s))
            return;

        try
        {
            using var encStream = new ByteStream(550);

            piranhaMessage.Encode(encStream);
            piranhaMessage.CustomEncode(encStream);

            using var stream = new ByteStream(600);

            stream.WriteVInt32(piranhaMessage.GetMessageType());
            stream.WriteVInt32(encStream.Length);
            stream.WriteBytesWithoutLength(encStream.GetBuffer());

            var encryptedPayload = s.Crypto.E(stream.GetBuffer());

            if (encryptedPayload == null)
                return;

            var finalPacket = new byte[10 + encryptedPayload.Length];

            BinaryPrimitives.WriteUInt64BigEndian(finalPacket.AsSpan(0, 8), sid.Low);
            BinaryPrimitives.WriteUInt16BigEndian(finalPacket.AsSpan(8, 2), sid.High);
            encryptedPayload.AsSpan().CopyTo(finalPacket.AsSpan(10));

            _socket.SendTo(finalPacket, SocketFlags.None, session.Ep);
        }
        catch (SocketException)
        {
            _sessions.TryRemove(sid, out _);
        }
        catch
        {
            // ignored.
        }
    }

    private async Task RunMetricsAndCleanupAsync(CancellationToken ct)
    {
        var cleanupCounter = 0;

        var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync(ct))
        {
            CurrentPps = Interlocked.Exchange(ref _packetCount, 0);

            if (++cleanupCounter < SessionTimeoutSeconds)
                continue;

            cleanupCounter = 0;

            var now = DateTime.UtcNow;

            foreach (var sessionId in _sessions.Keys)
                if (_sessions.TryGetValue(sessionId, out var data) &&
                    (now - data.LastSeen).TotalSeconds > SessionTimeoutSeconds)
                    _sessions.TryRemove(sessionId, out _);
        }
    }
}