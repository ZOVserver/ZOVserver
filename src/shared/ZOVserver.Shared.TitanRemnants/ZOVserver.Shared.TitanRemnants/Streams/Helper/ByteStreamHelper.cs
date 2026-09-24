using ZOVserver.Shared.TitanRemnants.Mathem.Shared;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.Streams.Helper;

public static class ByteStreamHelper
{
    public static LogicLong DecodeLogicLong(ByteStream byteStream)
    {
        var high = byteStream.ReadVInt32();
        var low = byteStream.ReadVInt32();

        return new LogicLong(high, low);
    }

    public static LogicLong EncodeLogicLong(ByteStream byteStream, LogicLong value)
    {
        byteStream.WriteVInt32(value.GetHigherInt());
        byteStream.WriteVInt32(value.GetLowerInt());

        return value;
    }

    public static int ReadDataReference(ByteStream byteStream)
    {
        var classId = byteStream.ReadVInt32();
        if (classId == 0) return classId;

        var instanceId = byteStream.ReadVInt32();

        return classId + instanceId < 1 ? 0 : GlobalId.CreateGlobalId(classId, instanceId);
    }

    public static int WriteDataReference(ByteStream byteStream, int globalId)
    {
        if (globalId > 0)
        {
            byteStream.WriteVInt32(GlobalId.GetClassId(globalId));
            byteStream.WriteVInt32(GlobalId.GetInstanceId(globalId));
        }
        else
        {
            byteStream.WriteVInt32(0);
        }

        return globalId;
    }

    public static List<int> ReadIntList(ByteStream byteStream)
    {
        var c = byteStream.ReadVInt32();

        var l = new List<int>();
        for (var i = 0; i < c; i++)
            l.Add(byteStream.ReadVInt32());

        return l;
    }

    public static void WriteIntList(ByteStream byteStream, List<int> list)
    {
        byteStream.WriteVInt32(list.Count);

        foreach (var t in list.ToArray())
            byteStream.WriteVInt32(t);
    }
}