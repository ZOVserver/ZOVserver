using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (1) at 1778788288 (fd3c74da-8eb8-495a-96e4-1f58854cc928).
public class LogicLocaleData(CsvElement csvElement) : LogicData(csvElement)
{
    public string IconSwf { get; } = csvElement.GetStringValue("IconSWF");
    public string IconExportName { get; } = csvElement.GetStringValue("IconExportName");
    public string LocalizedName { get; } = csvElement.GetStringValue("LocalizedName");
    public int SortOrder { get; } = csvElement.GetIntValue("SortOrder");
    public bool Enabled { get; } = csvElement.GetBoolValue("Enabled");
    public string FileName { get; } = csvElement.GetStringValue("FileName");
    public bool TestLanguage { get; } = csvElement.GetBoolValue("TestLanguage");
    public string UsedSystemFont { get; } = csvElement.GetStringValue("UsedSystemFont");
    public string PreferedFallbackFont { get; } = csvElement.GetStringValue("PreferedFallbackFont");
    public string ForcedFontFullName { get; } = csvElement.GetStringValue("ForcedFontFullName");
    public string HelpshiftSdkLanguage { get; } = csvElement.GetStringValue("HelpshiftSDKLanguage");
    public string HelpshiftSdkLanguageAndroid { get; } = csvElement.GetStringValue("HelpshiftSDKLanguageAndroid");
    public string TestExcludes { get; } = csvElement.GetStringValue("TestExcludes");
    public bool LoadAllLanguages { get; } = csvElement.GetBoolValue("LoadAllLanguages");
    public string ChampionshipRegisterUrl { get; } = csvElement.GetStringValue("ChampionshipRegisterUrl");
    public string ChampionshipLogo { get; } = csvElement.GetStringValue("ChampionshipLogo");
    public string TermsAndServiceUrl { get; } = csvElement.GetStringValue("TermsAndServiceUrl");
    public string ParentsGuideUrl { get; } = csvElement.GetStringValue("ParentsGuideUrl");
    public string PrivacyPolicyUrl { get; } = csvElement.GetStringValue("PrivacyPolicyUrl");
    public string LaserboxUrl { get; } = csvElement.GetStringValue("LaserboxUrl");
    public string LaserboxStagingUrl { get; } = csvElement.GetStringValue("LaserboxStagingUrl");
    public string LaserboxCommunityUrl { get; } = csvElement.GetStringValue("LaserboxCommunityUrl");
    public string LaserboxCommunityStagingUrl { get; } = csvElement.GetStringValue("LaserboxCommunityStagingUrl");
    public string FaqUrl_ios { get; } = csvElement.GetStringValue("FaqUrl_ios");
    public string FaqUrl_android { get; } = csvElement.GetStringValue("FaqUrl_android");
    public string ContactUsUrl_ios { get; } = csvElement.GetStringValue("ContactUsUrl_ios");
    public string ContactUsUrl_android { get; } = csvElement.GetStringValue("ContactUsUrl_android");
    public bool LaserboxEnabled { get; } = csvElement.GetBoolValue("LaserboxEnabled");
    public bool IsRtl { get; } = csvElement.GetBoolValue("IsRTL");
    public bool IsNounAdj { get; } = csvElement.GetBoolValue("isNounAdj");
    public bool SeparateThousandsWithSpaces { get; } = csvElement.GetBoolValue("SeparateThousandsWithSpaces");
    public string SelfHelpUrl { get; } = csvElement.GetStringValue("SelfHelpUrl");
    public bool FallbackToHelpshift { get; } = csvElement.GetBoolValue("FallbackToHelpshift");

    public override string ToString()
    {
        return $"""
                LogicLocaleData =>
                   Name = {Name},
                   IconSWF = {IconSwf},
                   IconExportName = {IconExportName},
                   LocalizedName = {LocalizedName},
                   SortOrder = {SortOrder},
                   Enabled = {Enabled},
                   FileName = {FileName},
                   TestLanguage = {TestLanguage},
                   UsedSystemFont = {UsedSystemFont},
                   PreferedFallbackFont = {PreferedFallbackFont},
                   ForcedFontFullName = {ForcedFontFullName},
                   HelpshiftSDKLanguage = {HelpshiftSdkLanguage},
                   HelpshiftSDKLanguageAndroid = {HelpshiftSdkLanguageAndroid},
                   TestExcludes = {TestExcludes},
                   LoadAllLanguages = {LoadAllLanguages},
                   ChampionshipRegisterUrl = {ChampionshipRegisterUrl},
                   ChampionshipLogo = {ChampionshipLogo},
                   TermsAndServiceUrl = {TermsAndServiceUrl},
                   ParentsGuideUrl = {ParentsGuideUrl},
                   PrivacyPolicyUrl = {PrivacyPolicyUrl},
                   LaserboxUrl = {LaserboxUrl},
                   LaserboxStagingUrl = {LaserboxStagingUrl},
                   LaserboxCommunityUrl = {LaserboxCommunityUrl},
                   LaserboxCommunityStagingUrl = {LaserboxCommunityStagingUrl},
                   FaqUrl_ios = {FaqUrl_ios},
                   FaqUrl_android = {FaqUrl_android},
                   ContactUsUrl_ios = {ContactUsUrl_ios},
                   ContactUsUrl_android = {ContactUsUrl_android},
                   LaserboxEnabled = {LaserboxEnabled},
                   IsRTL = {IsRtl},
                   isNounAdj = {IsNounAdj},
                   SeparateThousandsWithSpaces = {SeparateThousandsWithSpaces},
                   SelfHelpUrl = {SelfHelpUrl},
                   FallbackToHelpshift = {FallbackToHelpshift}
                """;
    }
}