using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Stream.Inheritors.Target;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Stream.Inheritors;

[MessagePackObject]
[LaserSerializable]
public partial class AllianceEventStreamEntry : StreamEntry
{
    [Key(100)]
    [Field(0, IsVarInt = true, BaseMethodBefore = true)]
    public int EventType { get; set; }

    [Key(101)]
    [Field(1, PresenceBool = true)]
    public EventStreamTargetEntry? TargetEntry { get; set; }

    public override int GetStreamEntryType()
    {
        return 4;
    }
}