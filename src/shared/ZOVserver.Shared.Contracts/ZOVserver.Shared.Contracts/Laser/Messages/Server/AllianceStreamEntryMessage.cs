using ZOVserver.Shared.Contracts.Laser.Combined.Stream;
using ZOVserver.Shared.Contracts.Laser.Combined.Stream.Factory;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

public class AllianceStreamEntryMessage : PiranhaMessage
{
    public StreamEntry? StreamEntry { get; set; }

    public override void CustomEncode(ByteStream stream)
    {
        base.CustomEncode(stream);

        if (StreamEntry == null) return;

        stream.WriteVInt32(StreamEntry.GetStreamEntryType());
        StreamEntry.Encode(stream);
    }

    public override void CustomDecode(ByteStream stream)
    {
        base.CustomDecode(stream);

        StreamEntry = StreamFactory.CreateStreamByType(stream.ReadVInt32());

        StreamEntry.Decode(stream);
    }

    public override int GetMessageType()
    {
        return 24312;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}