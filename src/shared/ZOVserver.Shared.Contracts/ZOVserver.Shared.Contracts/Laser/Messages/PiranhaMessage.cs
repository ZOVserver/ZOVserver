using MessagePack;
using Orleans;
using ZOVserver.Shared.Contracts.Laser.Machine;
using DebugInfoCollector = ZOVserver.Shared.Contracts.Laser.DebugInfo.DebugInfoCollector;

namespace ZOVserver.Shared.Contracts.Laser.Messages;

[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Laser.Messages.PiranhaMessage")]
public abstract class PiranhaMessage : LaserContract
{
    [IgnoreMember] [NonSerialized] private int _messageVersion = -1;
    [IgnoreMember] [NonSerialized] private int _proxySessionId;

    [IgnoreMember] [field: NonSerialized] public byte[]? Payload { get; init; }

    public virtual int GetMessageType()
    {
        return 0;
    }

    public virtual int GetServiceNodeType()
    {
        return 0;
    }

    public virtual string GetMessageTypeName()
    {
        return DebugInfoCollector.PacketCollectorY.GetValueOrDefault(GetMessageType(), ("UNKNOWN-NAME", -1)).Item1;
    }

    public virtual int GetMessageVersion()
    {
        return _messageVersion < 1 ? GetMessageType() == 20104 ? 1 : 0 : _messageVersion;
    }

    public void SetMessageVersion(int newArg)
    {
        _messageVersion = newArg;
    }

    public virtual int GetProxySessionId()
    {
        return _proxySessionId;
    }

    public virtual void SetProxySessionId(int proxySessionId)
    {
        _proxySessionId = proxySessionId;
    }

    public static bool IsClientToServerMessage(int messageType)
    {
        return messageType is >= 10000 and < 20000 or 30000;
    }

    public static bool IsServerToClientMessage(int messageType)
    {
        return messageType is >= 20000 and < 30000 or 40000;
    }
}