using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Team;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Stream.Inheritors;

[MessagePackObject]
[LaserSerializable]
public partial class QuickChatStreamEntry : StreamEntry
{
    [Key(100)]
    [Field(0, BaseMethodBefore = true)]
    public int MessageDataGlobalId { get; set; }

    [Key(101)]
    [Field(1, PresenceBool = true)]
    public TeamPremadeChatTargetPlayer? TargetPlayer { get; set; }

    [Key(102)] [Field(2)] public string UnkString { get; set; } = string.Empty;

    [Key(103)] [Field(3, IsVarInt = true)] public int EventSlot { get; set; }

    [Key(104)] [Field(4, IsVarInt = true)] public int DataId { get; set; }

    public override int GetStreamEntryType()
    {
        return 8;
    }
}