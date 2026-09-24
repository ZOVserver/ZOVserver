using ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Game;

namespace ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Extension;

public static class ShopItemHelperTableExtensions
{
    public static bool IsBox(this ShopItemHelperTable shopItemHelperTable)
    {
        return shopItemHelperTable is ShopItemHelperTable.FreeBox or ShopItemHelperTable.GuaranteedBox
            or ShopItemHelperTable.BrawlBox or ShopItemHelperTable.MediumBox or ShopItemHelperTable.BigBox
            or ShopItemHelperTable.AdBox;
    }
}