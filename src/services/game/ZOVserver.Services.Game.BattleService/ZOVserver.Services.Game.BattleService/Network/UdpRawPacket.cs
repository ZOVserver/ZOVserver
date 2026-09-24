using System.Net;

namespace ZOVserver.Services.Game.BattleService.Network;

public readonly record struct UdpRawPacket(byte[] Buffer, int Length, EndPoint Remote);