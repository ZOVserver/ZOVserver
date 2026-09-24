using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Analytic;

[LaserSerializable]
public partial class AnalyticEvent : LaserContract
{
    [Field(0)] public string Event { get; set; } = string.Empty;

    [Field(1)] public string EventInfo { get; set; } = string.Empty;
}