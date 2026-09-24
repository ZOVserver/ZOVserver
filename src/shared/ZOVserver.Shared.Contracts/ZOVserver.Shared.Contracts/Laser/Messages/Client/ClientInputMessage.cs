using ZOVserver.Shared.Contracts.Laser.Combined.Input;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

public class ClientInputMessage : PiranhaMessage
{
    public List<ClientInput> Inputs { get; set; } = new();

    public void Decode(ref BitStream bitStream)
    {
        bitStream.ReadPositiveIntMax16383();
        bitStream.ReadPositiveIntMax1023();
        bitStream.ReadPositiveIntMax8191();
        bitStream.ReadPositiveIntMax1023();

        var v7 = bitStream.ReadPositiveIntMax31();

        if (v7 < 1)
            return;

        for (var i = 0; i < v7; i++)
        {
            var input = new ClientInput();
            input.Decode(ref bitStream);

            Inputs.Add(input);
        }
    }

    public override int GetMessageType()
    {
        return 10555;
    }

    public override int GetServiceNodeType()
    {
        return 27;
    }
}