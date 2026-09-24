namespace ZOVserver.Shared.Contracts.Models;

public class OpenSearchAlliance
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int AllianceType { get; set; }

    public int RegionGlobalId { get; set; }
    public int LanguageGlobalId { get; set; }
    public int BadgeGlobalId { get; set; }

    public int NowTrophies { get; set; }
    public int RequiredTrophies { get; set; }

    public int MembersCount { get; set; }
}