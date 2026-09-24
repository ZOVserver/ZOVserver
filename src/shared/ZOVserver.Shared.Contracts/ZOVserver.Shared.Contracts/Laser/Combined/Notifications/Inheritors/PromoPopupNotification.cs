using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Entries;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Inheritors;

[MessagePackObject]
[LaserSerializable]
public partial class PromoPopupNotification : BaseNotification
{
    [Key(100)]
    [Field(0, BaseMethodBefore = true)]
    public ChronosTextEntry? Title { get; set; }

    [Key(101)] [Field(1)] public ChronosTextEntry? Description { get; set; }

    [Key(102)] [Field(2)] public ChronosTextEntry? ButtonText { get; set; }

    [Key(103)] [Field(3)] public ChronosFileEntry? Image { get; set; }

    [Key(104)] [Field(4)] public string? ButtonLink { get; set; }

    [Key(105)] [Field(5)] public int UnkVInt { get; set; }

    public override int GetNotificationType()
    {
        return 83;
    }
}