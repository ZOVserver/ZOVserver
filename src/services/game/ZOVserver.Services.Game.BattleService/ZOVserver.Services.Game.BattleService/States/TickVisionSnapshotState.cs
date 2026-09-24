namespace ZOVserver.Services.Game.BattleService.States;

public sealed class TickVisionSnapshotState
{
    public int SnapshotTick { get; init; }
    public int SnapshotViewers { get; init; }
    public bool SnapshotBrawlTv { get; init; }
    public byte[] SnapshotBitStreamBuffer { get; init; } = [];
}