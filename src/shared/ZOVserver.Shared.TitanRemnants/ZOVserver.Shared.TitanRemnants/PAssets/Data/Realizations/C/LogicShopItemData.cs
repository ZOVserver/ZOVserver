using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.C;

// Generated (45) at 1778788272 (6a10f1db-a4cf-4a70-9e4b-dd7129a228d3).
public class LogicShopItemData(CsvElement csvElement) : LogicData(csvElement)
{
    public int OfferType { get; } = csvElement.GetIntValue("OfferType");
    public int IconFrameNumber { get; } = csvElement.GetIntValue("IconFrameNumber");
    public int MaxResourcePerFrame { get; } = csvElement.GetIntValue("MaxResourcePerFrame");
    public string FileName { get; } = csvElement.GetStringValue("FileName");
    public string ShopItemAsset { get; } = csvElement.GetStringValue("ShopItemAsset");
    public string ShopItemBg { get; } = csvElement.GetStringValue("ShopItemBg");
    public string MiniOfferAsset { get; } = csvElement.GetStringValue("MiniOfferAsset");
    public string SeparatedSectionOfferAsset { get; } = csvElement.GetStringValue("SeparatedSectionOfferAsset");
    public string SeparatedSectionTeaseAsset { get; } = csvElement.GetStringValue("SeparatedSectionTeaseAsset");
    public string BrawlidaysOfferAsset { get; } = csvElement.GetStringValue("BrawlidaysOfferAsset");
    public string LegendaryOfferAsset { get; } = csvElement.GetStringValue("LegendaryOfferAsset");
    public string ConfirmItemAsset { get; } = csvElement.GetStringValue("ConfirmItemAsset");
    public string ConfirmItemBg { get; } = csvElement.GetStringValue("ConfirmItemBg");
    public string OfferAssetSmall { get; } = csvElement.GetStringValue("OfferAssetSmall");
    public string OfferAssetLarge { get; } = csvElement.GetStringValue("OfferAssetLarge");

    public override string ToString()
    {
        return $"""
                LogicShopItemData =>
                   Name = {Name},
                   OfferType = {OfferType},
                   IconFrameNumber = {IconFrameNumber},
                   MaxResourcePerFrame = {MaxResourcePerFrame},
                   FileName = {FileName},
                   ShopItemAsset = {ShopItemAsset},
                   ShopItemBg = {ShopItemBg},
                   MiniOfferAsset = {MiniOfferAsset},
                   SeparatedSectionOfferAsset = {SeparatedSectionOfferAsset},
                   SeparatedSectionTeaseAsset = {SeparatedSectionTeaseAsset},
                   BrawlidaysOfferAsset = {BrawlidaysOfferAsset},
                   LegendaryOfferAsset = {LegendaryOfferAsset},
                   ConfirmItemAsset = {ConfirmItemAsset},
                   ConfirmItemBg = {ConfirmItemBg},
                   OfferAssetSmall = {OfferAssetSmall},
                   OfferAssetLarge = {OfferAssetLarge}
                """;
    }
}