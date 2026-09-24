using ZOVserver.Shared.TitanRemnants.Streams;
using ZOVserver.Shared.TitanRemnants.Streams.Helper;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data;

public class LogicDataSlot
{
    public int DataGlobalId { get; set; }

    public int Count { get; set; }

    public void Decode(ByteStream byteStream)
    {
        DataGlobalId = ByteStreamHelper.ReadDataReference(byteStream);
        Count = byteStream.ReadVInt32();
    }

    public void Encode(ByteStream byteStream)
    {
        ByteStreamHelper.WriteDataReference(byteStream, DataGlobalId);
        byteStream.WriteVInt32(Count);
    }
}