using MessagePack;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;
using ZOVserver.Shared.TitanRemnants.Streams;
using ZOVserver.Shared.TitanRemnants.Streams.Helper;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;

[MessagePackObject]
public class LogicGemOffer
{
    [Key(0)] public int Type { get; set; }

    [Key(1)] public int Count { get; set; }

    [Key(2)] public int ItemGlobalId { get; set; }

    [Key(3)] public int ItemGlobalIdX { get; set; }

    public void CustomEncode(ByteStream byteStream)
    {
        byteStream.WriteVInt32(Type); // this + 0
        byteStream.WriteVInt32(ItemGlobalIdX > 0 ? 1 : Count); //  this + 4

        ByteStreamHelper.WriteDataReference(byteStream, ItemGlobalIdX > 0 ? 0 : ItemGlobalId); // this + 8

        byteStream.WriteVInt32(ItemGlobalIdX > 1000000
            ? GlobalId.GetInstanceId(ItemGlobalIdX)
            : ItemGlobalIdX); // this + 12
    }

    public void CustomDecode(ByteStream byteStream)
    {
        Type = byteStream.ReadVInt32();

        var encodedCount = byteStream.ReadVInt32();

        ItemGlobalId = ByteStreamHelper.ReadDataReference(byteStream);

        var encodedGlobalIdX = byteStream.ReadVInt32();

        if (ItemGlobalId > 0)
            Count = encodedCount == 1 ? encodedCount : Count;
        else
            Count = encodedCount;

        ItemGlobalIdX = encodedGlobalIdX;

        if (ItemGlobalIdX > 1000000)
            ItemGlobalIdX = GlobalId.GetInstanceId(ItemGlobalIdX);
    }

    public void SetItem(int itemGlobalId, bool isX = false) // isSkin
    {
        ItemGlobalId = itemGlobalId;
        if (isX) ItemGlobalIdX = itemGlobalId;
    }
}