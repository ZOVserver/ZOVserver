using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class MatchMakingStatusMessage : PiranhaMessage
{
    [Field(0)] public int Seconds { get; set; }
    [Field(1)] public int Found { get; set; }
    [Field(2)] public int Max { get; set; }
    [Field(3)] public int Unk1 { get; set; }
    [Field(4)] public int Unk2 { get; set; }
    [Field(5)] public bool ShowTips { get; set; }

    public override int GetMessageType()
    {
        return 20405;
    }

    public override int GetServiceNodeType()
    {
        return 4;
    }
}