using MessagePack;
using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Stream.Inheritors;

[MessagePackObject]
[LaserSerializable]
public partial class ChatStreamEntry : StreamEntry
{
    [Key(100)]
    [Field(0, BaseMethodBefore = true)]
    public string Text { get; set; } = string.Empty;

    public override int GetStreamEntryType()
    {
        return 2;
    }
}