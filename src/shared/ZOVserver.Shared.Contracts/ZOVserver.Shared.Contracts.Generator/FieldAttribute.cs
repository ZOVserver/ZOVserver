using System;

namespace ZOVserver.Shared.Contracts.Generator;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
#pragma warning disable CS9113 // Parameter is unread.
public class FieldAttribute(int order) : Attribute
#pragma warning restore CS9113 // Parameter is unread.
{
    public FieldType Type { get; set; } = FieldType.Auto;
    public bool IsVarInt { get; set; }
    public bool WriteLength { get; set; } = true;
    public bool Compressed { get; set; }
    public bool AsDataRef { get; set; }
    public bool PresenceBool { get; set; }
    public int RawLength { get; set; } = -1;
    public int AddNumber { get; set; }
    public bool CalculateSecondsLeft { get; set; }
    public bool CalculateSecondsPassed { get; set; }
    public bool LastOnlineTime { get; set; }
    public bool UseCustomContract { get; set; }
    public int[] DuplicateAfterOrders { get; set; } = [];
    public bool BaseMethodBefore { get; set; }
    public bool BaseMethodAfter { get; set; }
    public bool CountIsI32 { get; set; }
}