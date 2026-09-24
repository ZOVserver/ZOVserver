using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class AskForAllianceDataMessage : PiranhaMessage
{
    [Field(0)] public long AllianceId { get; set; }
    [Field(1, PresenceBool = true)] public UnkAskForAllianceDataMessagePart? Unk { get; set; }

    public override int GetMessageType()
    {
        return 14302;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}