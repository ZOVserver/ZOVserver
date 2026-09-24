using System.Net;

namespace ZOVserver.Services.Game.BattleService.Network;

public readonly struct UdpOutboundPacket(byte[] buffer, int length, IPEndPoint ep)
{
    public byte[] Buffer { get; } = buffer;
    public int Length { get; } = length;
    public IPEndPoint Ep { get; } = ep;
}