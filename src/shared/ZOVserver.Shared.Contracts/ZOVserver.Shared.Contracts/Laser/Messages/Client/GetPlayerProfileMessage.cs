using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Entries;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class GetPlayerProfileMessage : PiranhaMessage
{
    [Field(0)] public long AccountId { get; set; }
    [Field(1, PresenceBool = true)] public BattleLogPlayerEntry? BattleLogPlayerEntry { get; set; }

    public override int GetMessageType()
    {
        return 14113;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}