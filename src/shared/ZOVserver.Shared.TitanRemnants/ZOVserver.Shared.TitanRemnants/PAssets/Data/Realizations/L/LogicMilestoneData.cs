using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (39) at 1778788288 (c086d6bd-4bf5-433b-9cc4-953cb6f7be57).
public class LogicMilestoneData(CsvElement csvElement) : LogicData(csvElement)
{
    public int TypeInCsv { get; } = csvElement.GetIntValue("Type");
    public int Index { get; } = csvElement.GetIntValue("Index");
    public int ProgressStart { get; } = csvElement.GetIntValue("ProgressStart");
    public int Progress { get; } = csvElement.GetIntValue("Progress");
    public int League { get; } = csvElement.GetIntValue("League");
    public int Tier { get; } = csvElement.GetIntValue("Tier");
    public int Season { get; } = csvElement.GetIntValue("Season");
    public int SeasonEndRewardKeys { get; } = csvElement.GetIntValue("SeasonEndRewardKeys");
    public int PrimaryLvlUpRewardType { get; } = csvElement.GetIntValue("PrimaryLvlUpRewardType");
    public int PrimaryLvlUpRewardCount { get; } = csvElement.GetIntValue("PrimaryLvlUpRewardCount");
    public int PrimaryLvlUpRewardExtraData { get; } = csvElement.GetIntValue("PrimaryLvlUpRewardExtraData");
    public string PrimaryLvlUpRewardHero { get; } = csvElement.GetStringValue("PrimaryLvlUpRewardHero");
    public int SecondaryLvlUpRewardType { get; } = csvElement.GetIntValue("SecondaryLvlUpRewardType");
    public int SecondaryLvlUpRewardCount { get; } = csvElement.GetIntValue("SecondaryLvlUpRewardCount");
    public int SecondaryLvlUpRewardExtraData { get; } = csvElement.GetIntValue("SecondaryLvlUpRewardExtraData");
    public string SecondaryLvlUpRewardHero { get; } = csvElement.GetStringValue("SecondaryLvlUpRewardHero");

    public override string ToString()
    {
        return $"""
                LogicMilestoneData =>
                   Name = {Name},
                   Type = {TypeInCsv},
                   Index = {Index},
                   ProgressStart = {ProgressStart},
                   Progress = {Progress},
                   League = {League},
                   Tier = {Tier},
                   Season = {Season},
                   SeasonEndRewardKeys = {SeasonEndRewardKeys},
                   PrimaryLvlUpRewardType = {PrimaryLvlUpRewardType},
                   PrimaryLvlUpRewardCount = {PrimaryLvlUpRewardCount},
                   PrimaryLvlUpRewardExtraData = {PrimaryLvlUpRewardExtraData},
                   PrimaryLvlUpRewardHero = {PrimaryLvlUpRewardHero},
                   SecondaryLvlUpRewardType = {SecondaryLvlUpRewardType},
                   SecondaryLvlUpRewardCount = {SecondaryLvlUpRewardCount},
                   SecondaryLvlUpRewardExtraData = {SecondaryLvlUpRewardExtraData},
                   SecondaryLvlUpRewardHero = {SecondaryLvlUpRewardHero}
                """;
    }
}