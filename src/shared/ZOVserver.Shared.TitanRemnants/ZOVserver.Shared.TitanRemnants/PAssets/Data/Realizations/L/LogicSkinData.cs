using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (29) at 1778788288 (cdf68a3f-1ab8-4e18-ad0f-660cabad4d52).
public class LogicSkinData(CsvElement csvElement) : LogicData(csvElement)
{
    public string Conf { get; } = csvElement.GetStringValue("Conf");
    public int Campaign { get; } = csvElement.GetIntValue("Campaign");
    public int ObtainType { get; } = csvElement.GetIntValue("ObtainType");
    public string PetSkin { get; } = csvElement.GetStringValue("PetSkin");
    public string PetSkin2 { get; } = csvElement.GetStringValue("PetSkin2");
    public int CostLegendaryTrophies { get; } = csvElement.GetIntValue("CostLegendaryTrophies");
    public int CostGems { get; } = csvElement.GetIntValue("CostGems");
    public string Tid { get; } = csvElement.GetStringValue("TID");
    public string ShopTid { get; } = csvElement.GetStringValue("ShopTID");
    public string Features { get; } = csvElement.GetStringValue("Features");
    public string MaterialsFile { get; } = csvElement.GetStringValue("MaterialsFile");
    public string BlueTexture { get; } = csvElement.GetStringValue("BlueTexture");
    public string RedTexture { get; } = csvElement.GetStringValue("RedTexture");
    public string BlueSpecular { get; } = csvElement.GetStringValue("BlueSpecular");
    public string RedSpecular { get; } = csvElement.GetStringValue("RedSpecular");

    public override string ToString()
    {
        return $"""
                LogicSkinData =>
                   Name = {Name},
                   Conf = {Conf},
                   Campaign = {Campaign},
                   ObtainType = {ObtainType},
                   PetSkin = {PetSkin},
                   PetSkin2 = {PetSkin2},
                   CostLegendaryTrophies = {CostLegendaryTrophies},
                   CostGems = {CostGems},
                   TID = {Tid},
                   ShopTID = {ShopTid},
                   Features = {Features},
                   MaterialsFile = {MaterialsFile},
                   BlueTexture = {BlueTexture},
                   RedTexture = {RedTexture},
                   BlueSpecular = {BlueSpecular},
                   RedSpecular = {RedSpecular}
                """;
    }
}