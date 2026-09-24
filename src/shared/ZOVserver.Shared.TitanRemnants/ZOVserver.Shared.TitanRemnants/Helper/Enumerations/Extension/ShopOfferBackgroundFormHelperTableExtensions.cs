using ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Game;

namespace ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Extension;

public static class ShopOfferBackgroundFormHelperTableExtensions
{
    public static string GetBackgroundUiName(this ShopOfferBackgroundFormHelperTable shopOfferBackgroundFormHelperTable)
    {
        return shopOfferBackgroundFormHelperTable switch
        {
            ShopOfferBackgroundFormHelperTable.Generic => "offer_generic",
            ShopOfferBackgroundFormHelperTable.Special => "offer_special",
            ShopOfferBackgroundFormHelperTable.Legendary => "offer_legendary",
            ShopOfferBackgroundFormHelperTable.Coins => "offer_coins",
            ShopOfferBackgroundFormHelperTable.Gems => "offer_gems",
            ShopOfferBackgroundFormHelperTable.Boxes => "offer_boxes",
            ShopOfferBackgroundFormHelperTable.Finals => "offer_finals",
            ShopOfferBackgroundFormHelperTable.Xmas => "offer_xmas",
            ShopOfferBackgroundFormHelperTable.Lny => "offer_lny",
            ShopOfferBackgroundFormHelperTable.PinPack => "offer_pin_pack",
            ShopOfferBackgroundFormHelperTable.Chromatic => "offer_chromatic",
            ShopOfferBackgroundFormHelperTable.Archive => "offer_archive",
            ShopOfferBackgroundFormHelperTable.MoonFestival => "offer_moon_festival",
            ShopOfferBackgroundFormHelperTable.Wf => "offer_wf",
            ShopOfferBackgroundFormHelperTable.Lunar => "offer_lunar",
            ShopOfferBackgroundFormHelperTable.Lyn => "offer_lyn",
            ShopOfferBackgroundFormHelperTable.RandomEpic => "offer_random_epic",
            ShopOfferBackgroundFormHelperTable.Punk => "offer_punk",
            ShopOfferBackgroundFormHelperTable.Velocirapids => "offer_velocirapids",
            ShopOfferBackgroundFormHelperTable.Vault => "offer_vault",
            ShopOfferBackgroundFormHelperTable.Fairytale => "offer_fairytale",
            ShopOfferBackgroundFormHelperTable.Mf21 => "offer_mf21",
            ShopOfferBackgroundFormHelperTable.EpicPinPack => "offer_epic_pin_pack",
            ShopOfferBackgroundFormHelperTable.Retro => "offer_retro",
            ShopOfferBackgroundFormHelperTable.Brawloween => "offer_brawloween",
            ShopOfferBackgroundFormHelperTable.Brawlywood => "offer_brawlywood",
            ShopOfferBackgroundFormHelperTable.Blackfriday => "offer_blackfriday",
            ShopOfferBackgroundFormHelperTable.Singesday => "offer_singesday",
            ShopOfferBackgroundFormHelperTable.Brawlidays2021 => "offer_brawlidays2021",
            ShopOfferBackgroundFormHelperTable.Mrbeast => "offer_mrbeast",
            ShopOfferBackgroundFormHelperTable.Stv => "offer_stv",
            ShopOfferBackgroundFormHelperTable.Lny22 => "offer_lny22",
            ShopOfferBackgroundFormHelperTable.Biodome => "offer_biodome",
            ShopOfferBackgroundFormHelperTable.Easter22 => "offer_easter22",
            ShopOfferBackgroundFormHelperTable.Esports22 => "offer_esports22",
            ShopOfferBackgroundFormHelperTable.Ramadan22 => "offer_ramadan22",
            ShopOfferBackgroundFormHelperTable.Gw2022 => "offer_gw2022",
            ShopOfferBackgroundFormHelperTable.StarterPack => "offer_starter_pack",
            ShopOfferBackgroundFormHelperTable.Stuntshow => "offer_stuntshow",
            ShopOfferBackgroundFormHelperTable.Villains => "offer_villains",
            ShopOfferBackgroundFormHelperTable.Deepsea => "offer_deepsea",
            ShopOfferBackgroundFormHelperTable.Bt21 => "offer_bt21",
            ShopOfferBackgroundFormHelperTable.Moonfestival22 => "offer_moonfestival22",
            ShopOfferBackgroundFormHelperTable.Mecha => "offer_mecha",
            ShopOfferBackgroundFormHelperTable.Robotfactory => "offer_robotfactory",
            ShopOfferBackgroundFormHelperTable.Brawloween22 => "offer_brawloween22",
            ShopOfferBackgroundFormHelperTable.BgrLny22 => "offer_bgr_lny22",
            ShopOfferBackgroundFormHelperTable.BgrStuntshow => "offer_bgr_stuntshow",
            ShopOfferBackgroundFormHelperTable.BgrGw22 => "offer_bgr_gw22",
            ShopOfferBackgroundFormHelperTable.BgrVillains => "offer_bgr_villains",
            ShopOfferBackgroundFormHelperTable.BgrDeepsea => "offer_bgr_deepsea",
            ShopOfferBackgroundFormHelperTable.BgrBt21 => "offer_bgr_bt21",
            ShopOfferBackgroundFormHelperTable.BgrMecha => "offer_bgr_mecha",
            ShopOfferBackgroundFormHelperTable.BgrRobotfactory => "offer_bgr_robotfactory",
            ShopOfferBackgroundFormHelperTable.BgrBrawloween => "offer_bgr_brawloween",

            _ => throw new ArgumentOutOfRangeException(nameof(shopOfferBackgroundFormHelperTable),
                shopOfferBackgroundFormHelperTable, null)
        };
    }
}