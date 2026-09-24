using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Entries;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class BattleLogMessage : PiranhaMessage
{
    [Field(0)] public bool AllLogs { get; set; }
    [Field(1)] public BattleLogEntry[] BattleLogEntries { get; set; } = [];

    public override int GetMessageType()
    {
        return 23458;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}