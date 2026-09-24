using Orleans;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Team;

[LaserSerializable]
[GenerateSerializer]
[Alias("Team.TeamEntry")]
public partial class TeamEntry : LaserContract
{
    [Field(0, IsVarInt = true)] [Id(0)] public int RoomType { get; set; }

    [Field(1)] [Id(1)] public bool IsFriendlyRoom { get; set; }
    [Field(2, IsVarInt = true)] [Id(2)] public int MaxPlayers { get; set; }

    [Field(3)] [Id(3)] public long TeamId { get; set; }

    [Field(4, IsVarInt = true)] [Id(4)] public int Unk1 { get; set; }
    [Field(5)] [Id(5)] public bool Unk2 { get; set; }
    [Field(6)] [Id(6)] public bool Unk3 { get; set; }
    [Field(7, IsVarInt = true)] [Id(7)] public int Unk4 { get; set; }
    [Field(8, IsVarInt = true)] [Id(8)] public int Unk5 { get; set; }

    [Field(9, AsDataRef = true)] [Id(9)] public int LocationGlobalId { get; set; }

    [Field(10)] [Id(10)] public List<TeamMemberEntry> Members { get; set; } = [];
    [Field(11)] [Id(11)] public List<TeamInviteEntry> Invites { get; set; } = [];
    [Field(12)] [Id(12)] public List<TeamJoinRequest> JoinRequests { get; set; } = [];

    [Field(13)] [Id(13)] public bool IsClubWar { get; set; }
    [Field(14)] [Id(14)] public bool TextChatEnabled { get; set; } = true;

    [Id(15)] public int LastMemberIndexesType { get; set; }
    [Id(16)] public int GameModeVariation { get; set; }
    [Id(17)] public int EventSlot { get; set; }
}