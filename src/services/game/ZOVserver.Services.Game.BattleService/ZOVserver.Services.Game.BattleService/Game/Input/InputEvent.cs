using ZOVserver.Services.Game.BattleService.Network;
using ZOVserver.Shared.Contracts.Laser.Combined.Input;

namespace ZOVserver.Services.Game.BattleService.Game.Input;

public readonly struct InputEvent(SessionId sessionId, ClientInput input)
{
    public readonly SessionId SessionId = sessionId;
    public readonly ClientInput Input = input;
}