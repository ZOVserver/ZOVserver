using ZOVserver.Shared.Contracts.Laser.Combined.Stream;
using ZOVserver.Shared.Contracts.Laser.Combined.Stream.Factory;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

public class TeamStreamMessage : PiranhaMessage
{
    public List<StreamEntry> StreamEntries = [];
    public long TeamId { get; set; }

    public override void CustomEncode(ByteStream stream)
    {
        base.CustomEncode(stream);

        stream.WriteVInt64(TeamId);

        stream.WriteVInt32(StreamEntries.Count);

        foreach (var streamEntry in StreamEntries)
        {
            stream.WriteVInt32(streamEntry.GetStreamEntryType());

            streamEntry.Encode(stream);
        }
    }

    public override void CustomDecode(ByteStream stream)
    {
        base.CustomDecode(stream);

        TeamId = stream.ReadVInt64();

        var count = stream.ReadVInt32();

        for (var i = 0; i < count; i++)
        {
            var streamEntry = StreamFactory.CreateStreamByType(stream.ReadVInt32());

            streamEntry.Decode(stream);

            StreamEntries.Add(streamEntry);
        }
    }

    public override int GetMessageType()
    {
        return 24131;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}