using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Stream.Inheritors;

[MessagePackObject]
[LaserSerializable]
public partial class JoinRequestAllianceStreamEntry : StreamEntry
{
    [Key(100)]
    [Field(0, BaseMethodBefore = true)]
    public string Text { get; set; } = string.Empty;

    [Key(101)] [Field(1)] public string ResponderName { get; set; } = string.Empty;

    [Key(102)] [Field(2, IsVarInt = true)] public int State { get; set; }

    [Key(103)] [Field(3)] public PlayerDisplayData? DisplayData { get; set; }

    public override int GetStreamEntryType()
    {
        return 3;
    }
}