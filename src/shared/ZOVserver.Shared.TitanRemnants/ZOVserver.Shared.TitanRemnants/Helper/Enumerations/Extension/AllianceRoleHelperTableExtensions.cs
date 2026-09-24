using ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Game;

namespace ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Extension;

public static class AllianceRoleHelperTableExtensions
{
    public static string GetCsvName(this AllianceRoleHelperTable allianceRoleHelperTable)
    {
        return allianceRoleHelperTable switch
        {
            AllianceRoleHelperTable.NonMember => "NonMember",
            AllianceRoleHelperTable.Member => "Member",
            AllianceRoleHelperTable.Elder => "Elder",
            AllianceRoleHelperTable.CoLeader => "Co-leader",
            AllianceRoleHelperTable.Leader => "Leader",
            _ => throw new ArgumentOutOfRangeException(nameof(allianceRoleHelperTable), allianceRoleHelperTable, null)
        };
    }
}