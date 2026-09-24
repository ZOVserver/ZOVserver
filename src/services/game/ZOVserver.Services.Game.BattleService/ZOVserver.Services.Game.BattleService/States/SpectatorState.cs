using ZOVserver.Services.Game.BattleService.Network;

namespace ZOVserver.Services.Game.BattleService.States;

public class SpectatorState
{
    public required long AccountId;
    public required int LastInputIndex;
    public required SessionId SessionId;
}