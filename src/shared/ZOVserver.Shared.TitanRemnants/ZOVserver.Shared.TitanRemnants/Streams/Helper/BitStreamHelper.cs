using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.Streams.Helper;

public static class BitStreamHelper
{
    public static int ReadDataReference(ref BitStream bitStream)
    {
        var classId = (int)bitStream.ReadPositiveIntMax31();
        if (classId <= 0) return 0;

        var instanceId = (int)bitStream.ReadPositiveIntMax255();
        return GlobalId.CreateGlobalId(classId, instanceId);
    }

    public static int WriteDataReference(ref BitStream bitStream, int globalId)
    {
        if (GlobalId.GetClassId(globalId) > 0)
        {
            bitStream.WritePositiveIntMax31((uint)GlobalId.GetClassId(globalId));
            bitStream.WritePositiveIntMax255((uint)GlobalId.GetInstanceId(globalId));
        }
        else
        {
            bitStream.WritePositiveIntMax31(0);
        }

        return globalId;
    }

    public static int ReadObjectRunningId(ref BitStream bitStream)
    {
        return (int)bitStream.ReadPositiveIntMax16383();
    }

    public static void WriteObjectRunningId(ref BitStream bitStream, int globalId)
    {
        bitStream.WritePositiveIntMax16383((uint)GlobalId.GetInstanceId(globalId));
    }
}