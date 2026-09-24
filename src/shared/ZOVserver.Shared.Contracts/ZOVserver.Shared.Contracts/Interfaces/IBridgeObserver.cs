using Orleans;
using Orleans.Concurrency;
using ZOVserver.Shared.Contracts.Events;

namespace ZOVserver.Shared.Contracts.Interfaces;

[Alias("ZOVserver.Shared.Contracts.Interfaces.IBridgeObserver")]
public interface IBridgeObserver : IGrainObserver
{
    [Alias("SendBridgeEvent")]
    [OneWay]
    Task SendBridgeEventAsync(BridgeEvent bridgeEvent);
}