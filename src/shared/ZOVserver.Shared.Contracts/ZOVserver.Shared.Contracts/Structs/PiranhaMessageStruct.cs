using Orleans;

namespace ZOVserver.Shared.Contracts.Structs;

[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Structs.PiranhaMessageStruct")]
public struct PiranhaMessageStruct
{
    [Id(0)] public int MessageType { get; set; }

    [Id(1)] public int MessageVersion { get; set; }

    [Id(2)] public string MessageName { get; set; }

    [Id(3)] public byte[] MessagePayload { get; set; }
}