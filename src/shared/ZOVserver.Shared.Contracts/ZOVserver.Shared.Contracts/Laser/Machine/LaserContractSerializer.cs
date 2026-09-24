using ZOVserver.Shared.Contracts.Laser.Messages;
using ZOVserver.Shared.Contracts.Structs;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Shared.Contracts.Laser.Machine;

public static class LaserContractSerializer
{
    public static byte[] Serialize<T>(T contract, bool alsoUseCustomContract = true) where T : LaserContract
    {
        var byteStream = new ByteStream(contract.Capacity);

        contract.Encode(byteStream);

        if (alsoUseCustomContract)
            contract.CustomEncode(byteStream);

        return byteStream.GetBuffer().ToArray();
    }

    public static PiranhaMessageStruct SerializeToStruct<T>(T contract, bool alsoUseCustomContract = true)
        where T : PiranhaMessage
    {
        return new PiranhaMessageStruct
        {
            MessageType = contract.GetMessageType(),
            MessageName = contract.GetMessageTypeName(),
            MessageVersion = contract.GetMessageVersion(),
            MessagePayload = Serialize(contract, alsoUseCustomContract)
        };
    }

    public static T Deserialize<T>(byte[] buffer, bool alsoUseCustomContract = true) where T : LaserContract, new()
    {
        var byteStream = new ByteStream(buffer);

        var contract = new T();

        contract.Decode(byteStream);

        if (alsoUseCustomContract)
            contract.CustomDecode(byteStream);

        byteStream.Dispose();

        return contract;
    }

    public static void Deserialize(LaserContract contract, byte[] buffer, bool alsoUseCustomContract = true)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(contract);

        var byteStream = new ByteStream(buffer);

        contract.Decode(byteStream);

        if (alsoUseCustomContract)
            contract.CustomDecode(byteStream);

        byteStream.Dispose();
    }
}