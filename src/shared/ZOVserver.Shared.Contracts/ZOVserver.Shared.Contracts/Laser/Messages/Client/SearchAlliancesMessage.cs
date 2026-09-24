using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class SearchAlliancesMessage : PiranhaMessage
{
    [Field(0)] public string Name { get; set; } = string.Empty;
    [Field(1)] public int Unk1 { get; set; }
    [Field(2)] public int MinMembers { get; set; }
    [Field(3)] public int MaxMembers { get; set; }
    [Field(4)] public int MinTrophies { get; set; }
    [Field(5)] public bool FindOnlyJoinableClubs { get; set; }
    [Field(6)] public int Score { get; set; }
    [Field(7)] public int MinLevel { get; set; }

    public override int GetMessageType()
    {
        return 14324;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}