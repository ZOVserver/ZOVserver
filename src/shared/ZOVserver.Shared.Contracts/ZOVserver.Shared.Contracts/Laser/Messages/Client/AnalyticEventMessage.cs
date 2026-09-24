using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Analytic;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class AnalyticEventMessage : PiranhaMessage
{
    [Field(0)] public AnalyticEvent? AnalyticEvent { get; set; }

    public override int GetMessageType()
    {
        return 10110;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}