using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.C;

// Generated (2) at 1778788272 (5200b23b-805c-49f8-943c-2470db127a00).
public class LogicBillingPackageData(CsvElement csvElement) : LogicData(csvElement)
{
    public string Tid { get; } = csvElement.GetStringValue("TID");
    public int TypeInCsv { get; } = csvElement.GetIntValue("Type");
    public bool Disabled { get; } = csvElement.GetBoolValue("Disabled");
    public bool ExistsApple { get; } = csvElement.GetBoolValue("ExistsApple");
    public bool ExistsAndroid { get; } = csvElement.GetBoolValue("ExistsAndroid");
    public int Diamonds { get; } = csvElement.GetIntValue("Diamonds");
    public int USd { get; } = csvElement.GetIntValue("USD");
    public int Order { get; } = csvElement.GetIntValue("Order");
    public string IconExportName { get; } = csvElement.GetStringValue("IconExportName");
    public int FrameNumber { get; } = csvElement.GetIntValue("FrameNumber");
    public int StarterPackNumber { get; } = csvElement.GetIntValue("StarterPackNumber");
    public int BigBoxCount { get; } = csvElement.GetIntValue("BigBoxCount");
    public int XpLevelReq { get; } = csvElement.GetIntValue("XpLevelReq");
    public int ValueFactor { get; } = csvElement.GetIntValue("ValueFactor");
    public string LabelTid { get; } = csvElement.GetStringValue("LabelTID");
    public int LabelValue { get; } = csvElement.GetIntValue("LabelValue");
    public int Bg { get; } = csvElement.GetIntValue("Bg");
    public int Decor { get; } = csvElement.GetIntValue("Decor");
    public bool IsPromotion { get; } = csvElement.GetBoolValue("IsPromotion");
    public int Coins { get; } = csvElement.GetIntValue("Coins");
    public int RefundGemValue { get; } = csvElement.GetIntValue("RefundGemValue");
    public int BrawlPassSeason { get; } = csvElement.GetIntValue("BrawlPassSeason");

    public override string ToString()
    {
        return $"""
                LogicBillingPackageData =>
                   Name = {Name},
                   TID = {Tid},
                   Type = {TypeInCsv},
                   Disabled = {Disabled},
                   ExistsApple = {ExistsApple},
                   ExistsAndroid = {ExistsAndroid},
                   Diamonds = {Diamonds},
                   USD = {USd},
                   Order = {Order},
                   IconExportName = {IconExportName},
                   FrameNumber = {FrameNumber},
                   StarterPackNumber = {StarterPackNumber},
                   BigBoxCount = {BigBoxCount},
                   XpLevelReq = {XpLevelReq},
                   ValueFactor = {ValueFactor},
                   LabelTID = {LabelTid},
                   LabelValue = {LabelValue},
                   Bg = {Bg},
                   Decor = {Decor},
                   IsPromotion = {IsPromotion},
                   Coins = {Coins},
                   RefundGemValue = {RefundGemValue},
                   BrawlPassSeason = {BrawlPassSeason}
                """;
    }
}