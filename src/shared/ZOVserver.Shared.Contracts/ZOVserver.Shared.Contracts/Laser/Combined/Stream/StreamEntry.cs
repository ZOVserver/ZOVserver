using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Stream.Inheritors;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Stream;

[Union(2, typeof(ChatStreamEntry))]
[Union(3, typeof(JoinRequestAllianceStreamEntry))]
[Union(4, typeof(AllianceEventStreamEntry))]
[Union(6, typeof(MessageDataStreamEntry))]
[Union(8, typeof(QuickChatStreamEntry))]
[MessagePackObject]
[LaserSerializable]
public abstract partial class StreamEntry : LaserContract
{
    [Key(0)] [Field(0, IsVarInt = true)] public long StreamEntryId { get; set; }

    [Key(1)] [Field(1, IsVarInt = true)] public long AuthorId { get; set; }

    [Key(2)] [Field(2)] public string AuthorName { get; set; } = string.Empty;

    [Key(3)] [Field(3, IsVarInt = true)] public int AuthorRole { get; set; }

    [Key(4)]
    [Field(4, IsVarInt = true, CalculateSecondsPassed = true)]
    public DateTime SendTime { get; set; }

    [Key(5)] [Field(5)] public bool Invisible { get; set; }

    public abstract int GetStreamEntryType();
}