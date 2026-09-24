using ZOVserver.Shared.Contracts.Laser.Commands;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

public class EndClientTurnMessage : PiranhaMessage
{
    public int Tick { get; set; }
    public int Checksum { get; set; }
    public List<(int, LogicCommand?)> Commands { get; set; } = [];

    public override void CustomEncode(ByteStream stream)
    {
        stream.WriteBoolean(false);

        stream.WriteVInt32(Tick);
        stream.WriteVInt32(Checksum);

        stream.WriteVInt32(Commands.Count);
        foreach (var t in Commands)
            LogicCommandManager.EncodeCommand(t.Item2!, stream);

        stream.WriteBytes([]);
    }

    public override void CustomDecode(ByteStream stream)
    {
        if (stream.Length == 0) return;

        _ = stream.ReadBoolean();

        Tick = stream.ReadVInt32();
        Checksum = stream.ReadVInt32();

        var commands = stream.ReadVInt32();
        for (var i = 0; i < commands; i++)
        {
            var command = LogicCommandManager.DecodeCommand(stream);
            if (command == (555041, null)) continue;
            Commands.Add(command);
        }

        stream.ReadBytes();
    }

    public override int GetMessageType()
    {
        return 14102;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}