using ZOVserver.Shared.Contracts.Models;

namespace ZOVserver.Services.Game.AllianceService.States;

public class AllianceState
{
    public long AllianceId { get; set; }

    public long OwnerAccountId { get; set; }
    public DateTime? CreatedDateTime { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int AllianceType { get; set; }

    public int RegionGlobalId { get; set; }
    public int LanguageGlobalId { get; set; }
    public int BadgeGlobalId { get; set; }

    public int CalculatedSumTrophies { get; set; }
    public int RequiredTrophies { get; set; }

    public Dictionary<long, AllianceMember> Members { get; set; } = [];
    public Dictionary<long, DateTime> BannedMembers { get; set; } = [];
}