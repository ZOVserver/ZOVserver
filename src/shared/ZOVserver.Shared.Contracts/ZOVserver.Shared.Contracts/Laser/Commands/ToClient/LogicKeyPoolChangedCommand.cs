using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Commands.ToClient;

[LaserSerializable]
public partial class LogicKeyPoolChangedCommand : LogicServerCommand
{
    [Field(0, IsVarInt = true)] public int AvailableBattleTokens { get; set; }

    [Field(1, IsVarInt = true, BaseMethodAfter = true)]
    public int SecondsToNextBattleTokens { get; set; }

    public override int GetCommandType()
    {
        return 209;
    }
}