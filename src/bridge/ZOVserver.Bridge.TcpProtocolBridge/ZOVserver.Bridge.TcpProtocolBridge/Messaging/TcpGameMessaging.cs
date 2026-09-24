using System.Buffers.Binary;
using NLog;
using ZOVserver.Bridge.TcpProtocolBridge.Abstractions;
using ZOVserver.Bridge.TcpProtocolBridge.Sessions;
using ZOVserver.RasputinProtect;
using ZOVserver.Shared.Contracts.Laser.DebugInfo;
using ZOVserver.Shared.Contracts.Laser.Machine;
using ZOVserver.Shared.Contracts.Laser.Messages;
using ZOVserver.Shared.Contracts.Laser.Messages.Client;
using ZOVserver.Shared.Contracts.Laser.Messages.Server;
using static ZOVserver.Bridge.TcpHellomateGirl.TcpHellomateGirl;

namespace ZOVserver.Bridge.TcpProtocolBridge.Messaging;

public class TcpGameMessaging(LaserHighLevelTcpSession session) : ITcpGameMessaging
{
    private const bool UseCrypto = true;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private MemoryStream? _buffer = new();
    private SemaphoreSlim? _encryptionSemaphore = new(1, 1);
    private int _pepperState = 2;

    private GimmeGimmeGimme? _piu;

    public async Task ReceiveBufferAsync(byte[] buffer)
    {
        if (IsClientHello(buffer))
        {
            await session.LowLevelSession.SendBufferAsync(GetServerHelloWithKey());
            return;
        }

        var data = await ProcessBufferAsync(buffer);

        if (data.dangerous)
        {
            await session.LowLevelSession.DisconnectAsync();
            return;
        }

        var messagesToGrain = new List<PiranhaMessage>();

        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var message in data.messages)
        {
            Logger.Trace($"Message ({message.messageType}) received!");

            var res = await ReadNewMessage(message.messageType, message.messageLength, message.messageVersion,
                message.payload);

            switch (res.Item1)
            {
                case 5204580 when res.Item2?.Payload != null:
                    messagesToGrain.Add(res.Item2);
                    continue;
                case 0:
                    continue;
            }

            Logger.Warn($"Message ({message.messageType}) unsuccessfully processed with code: {res}!");
            await session.LowLevelSession.DisconnectAsync();
            return;
        }

        if (messagesToGrain.Count > 0)
        {
            var messageManager = session.GetGameMessageManager();

            if (messageManager == null)
            {
                await session.LowLevelSession.DisconnectAsync();
                return;
            }

            var res = await messageManager.ReceiveMaybeImplementedMessagesByGrains(messagesToGrain);

            if (res < 0)
                await session.LowLevelSession.DisconnectAsync();
        }
    }

    public async Task SendAsync(PiranhaMessage piranhaMessage)
    {
        if (piranhaMessage.GetMessageType() == 20100 && UseCrypto)
        {
            if (_pepperState != 3) return;

            var serverHelloMessage = (ServerHelloMessage)piranhaMessage;

            _piu = GoodbyeMyLoveGoodbye.CreateS88();

            var t = _piu.S1(serverHelloMessage.ChsByte);

            if (t == null)
            {
                await session.LowLevelSession.DisconnectAsync();
                return;
            }

            serverHelloMessage.ServerHelloToken = t;
        }

        var messageBytes = LaserContractSerializer.Serialize(piranhaMessage);

        if (await EncryptAndWriteAsync(piranhaMessage.GetMessageType(), piranhaMessage.GetMessageVersion(),
                messageBytes) != 0)
            await session.LowLevelSession.DisconnectAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_buffer != null) await _buffer.DisposeAsync();
        _buffer = null;

        _encryptionSemaphore?.Dispose();
        _encryptionSemaphore = null;

        _pepperState = 0;

        _piu?.Dispose();
        _piu = null;
    }

    public async Task<int> EncryptAndWriteAsync(int type, int version, byte[]? payload)
    {
        if (_encryptionSemaphore == null)
            return -1;

        await _encryptionSemaphore.WaitAsync();

        try
        {
            if (payload == null) return -1;
            if (!PiranhaMessage.IsServerToClientMessage(type)) return -1;

            // ReSharper disable once HeuristicUnreachableCode
#pragma warning disable CS0162 // Unreachable code detected
            if (!UseCrypto) goto label_1;
#pragma warning restore CS0162 // Unreachable code detected

            switch (_pepperState)
            {
                case 4:
                {
                    if (_piu == null)
                        return -1;

                    var pld = _piu.S3(payload);

                    if (pld == null)
                        return -1;

                    payload = pld;
                    _pepperState = 5;
                    break;
                }
                case 5:
                {
                    payload = _piu?.E(payload);

                    if (payload == null)
                        return -1;

                    break;
                }
            }

            label_1:
            var message = WriteHeader(payload, type, version);
            payload.CopyTo(message.AsSpan(7));

            await session.LowLevelSession.SendBufferAsync(message);

            Logger.Debug(
                $"New message has been sent: {DebugInfoCollector.PacketCollectorY.GetValueOrDefault(type, ("unknown", -1))}.");

            return 0;
        }
        catch (Exception exception)
        {
            Logger.Error(exception.ToString());
            return -1;
        }
        finally
        {
            _encryptionSemaphore.Release();
        }
    }

    private async Task<(List<(ushort messageType, ushort messageVersion, int messageLength, byte[] payload)> messages,
            bool dangerous)>
        ProcessBufferAsync(ReadOnlyMemory<byte> unsegmentedBuffer)
    {
        if (_buffer == null)
            return ([], true);

        var parsedMessages = new List<(ushort messageType, ushort messageVersion, int messageLength, byte[] payload)>();
        var dangerous = false;

        if (unsegmentedBuffer.Length is < 1 or > 8191)
        {
            dangerous = true;
            goto ret;
        }

        await _buffer.WriteAsync(unsegmentedBuffer);

        var bufferSpan = _buffer.GetBuffer().AsSpan(0, (int)_buffer.Length);

        while (bufferSpan.Length >= 7)
        {
            var headerSpan = bufferSpan[..7];

            var messageType = BinaryPrimitives.ReadUInt16BigEndian(headerSpan[..2]);
            var messageLength = (headerSpan[2] << 16) | (headerSpan[3] << 8) | headerSpan[4];
            var messageVersion = BinaryPrimitives.ReadUInt16BigEndian(headerSpan.Slice(5, 2));

            if (messageLength is < 0 or > 2048)
            {
                dangerous = true;
                break;
            }

            if (bufferSpan.Length < 7 + messageLength)
                break;

            var payload = bufferSpan.Slice(7, messageLength).ToArray();
            parsedMessages.Add((messageType, messageVersion, messageLength, payload));

            bufferSpan = bufferSpan[(7 + messageLength)..];
        }

        _buffer.SetLength(0);

        if (!dangerous)
            await _buffer.WriteAsync(bufferSpan.ToArray());

        ret:
        return (parsedMessages, dangerous);
    }

    private async Task<(int, PiranhaMessage?)> ReadNewMessage(int type, int length, int version, byte[]? payload)
    {
        if (version is < 0 or > 65536)
            return (-601, null);

        // ReSharper disable once HeuristicUnreachableCode
#pragma warning disable CS0162 // Unreachable code detected
        if (!UseCrypto) goto label_1;
#pragma warning restore CS0162 // Unreachable code detected

        switch (_pepperState)
        {
            case 2:
            {
                if (type == 10100) _pepperState = 3;
                else return (-602, null);
                break;
            }
            case 3:
            {
                if (type != 10101) return (-603, null);
                if (_piu == null) return (-604, null);

                payload = _piu.S2(payload);

                if (payload == null)
                    return (-607, null);

                _pepperState = 4;
                break;
            }
            case 5:
            {
                if (_piu == null)
                    return (-608, null);

                if ((payload = _piu.D(payload)) == null)
                {
                    Logger.Warn(
                        $"New crime message received: {type}.");
                    return (-609, null);
                }

                break;
            }
        }

        label_1:

        if (payload == null) return (-610, null);

        var lr = type is 10099 or 10100 or 10101 or 10108;

        PiranhaMessage? piranhaMessage;

        if (lr)
        {
            piranhaMessage = LogicLaserMessageFactory.CreateMessageByType(type);
            {
                if (piranhaMessage == null)
                {
                    Logger.Warn(
                        $"New unknown message received: {DebugInfoCollector.PacketCollectorY.GetValueOrDefault(type, ("unknown???", -1))}.");
                    return (0, null);
                }
            }
        }
        else
        {
            piranhaMessage = new DynamicClientMessage(type, 0) { Payload = payload };
        }

        Logger.Debug(
            $"New message (localRcv={lr}) received: {DebugInfoCollector.PacketCollectorY.GetValueOrDefault(type, ("unknown", -1))}.");

        if (!lr) return (5204580, piranhaMessage);

        LaserContractSerializer.Deserialize(piranhaMessage, payload);

        var manager = session.GetGameMessageManager();

        return manager == null ? (-611, null) : (await manager.ReceiveMessageAsync(piranhaMessage), piranhaMessage);
    }

    private static byte[] WriteHeader(ReadOnlySpan<byte> payload, int messageType, int messageVersion)
    {
        var plength = payload.Length;

        var header = GC.AllocateUninitializedArray<byte>(7 + plength);
        {
            header[0] = (byte)(messageType >> 8);
            header[1] = (byte)messageType;
            header[2] = (byte)(plength >> 16);
            header[3] = (byte)(plength >> 8);
            header[4] = (byte)plength;
            header[5] = (byte)(messageVersion >> 8);
            header[6] = (byte)messageVersion;
        }

        return header;
    }
}