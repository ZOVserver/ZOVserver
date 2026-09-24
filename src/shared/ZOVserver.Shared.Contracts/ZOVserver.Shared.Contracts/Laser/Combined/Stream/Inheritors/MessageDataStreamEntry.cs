using MessagePack;
using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Stream.Inheritors;

[MessagePackObject]
[LaserSerializable]
public partial class MessageDataStreamEntry : StreamEntry
{
    [Key(100)]
    [Field(0, BaseMethodBefore = true)]
    public int MessageDataGlobalId { get; set; }

    public override int GetStreamEntryType()
    {
        return 6;
    }
}