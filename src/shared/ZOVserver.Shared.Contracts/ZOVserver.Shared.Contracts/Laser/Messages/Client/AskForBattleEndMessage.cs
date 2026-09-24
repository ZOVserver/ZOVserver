using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Entries;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class AskForBattleEndMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public int BattleResult { get; set; }

    [Field(1, IsVarInt = true)] public int Result { get; set; }

    [Field(2, IsVarInt = true)] public int Rank { get; set; }

    [Field(3)] public int LocationGlobalId { get; set; }

    [Field(4)] public List<HeroDataEntry> Heroes { get; set; } = [];

    public override int GetMessageType()
    {
        return 14110;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}