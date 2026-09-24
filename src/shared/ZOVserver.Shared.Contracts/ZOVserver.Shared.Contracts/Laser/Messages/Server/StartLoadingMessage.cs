using Orleans;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Laser.Messages.Server.StartLoadingMessage")]
public partial class StartLoadingMessage : PiranhaMessage
{
    [Field(0)] [Id(0)] public int PlayersCount { get; set; }

    [Field(1)] [Id(1)] public int MyPlayerIndex { get; set; }
    [Field(2)] [Id(2)] public int MyTeamIndex { get; set; }

    [Field(3, CountIsI32 = true)] [Id(3)] public LogicPlayer[] Players { get; set; } = [];

    [Field(4)] [Id(4)] public int UnkArray { get; set; }

    [Field(5, CountIsI32 = true)] [Id(5)] public int[] EventModifiers { get; set; } = [];

    [Field(6)] [Id(6)] public int Seed { get; set; }

    [Field(7, IsVarInt = true)] [Id(7)] public int GameType { get; set; }

    [Field(8, IsVarInt = true)] [Id(8)] public int MapType { get; set; }

    [Field(9, IsVarInt = true)] [Id(9)] public int ControlType { get; set; }

    [Field(10)] [Id(10)] public bool GameHintsEnabled { get; set; }

    [Field(11, IsVarInt = true)] [Id(11)] public int SpectateMode { get; set; }

    [Field(12, IsVarInt = true)] [Id(12)] public int RaidDifficulty { get; set; }

    [Field(13, AsDataRef = true)] [Id(13)] public int LocationGlobalId { get; set; }

    public override int GetMessageType()
    {
        return 20559;
    }

    public override int GetServiceNodeType()
    {
        return 4;
    }
}