using ZOVserver.Shared.Contracts.Laser.Commands;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

public class AvailableServerCommandMessage : PiranhaMessage
{
    private LogicCommand _logicCommand = null!;

    public override void CustomEncode(ByteStream stream)
    {
        LogicCommandManager.EncodeCommand(_logicCommand, stream);
    }

    public override void Decode(ByteStream stream)
    {
        var (_, logicCommand) = LogicCommandManager.DecodeCommand(stream);
        _logicCommand = logicCommand!;
    }

    public void SetServerCommand(LogicServerCommand logicCommand)
    {
        _logicCommand = logicCommand;
    }

    public override int GetMessageType()
    {
        return 24111;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}