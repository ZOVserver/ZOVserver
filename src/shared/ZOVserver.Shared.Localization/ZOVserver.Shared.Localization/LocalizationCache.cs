using System.Collections.Frozen;
using Newtonsoft.Json;
using ZOVserver.Shared.Contracts.Proto;

namespace ZOVserver.Shared.Localization;

public sealed class LocalizationCache
{
    private static FrozenDictionary<string, FrozenDictionary<uint, string>> _cachedTranslations = null!;

    private static readonly FrozenDictionary<string, int> LanguageToId = new Dictionary<string, int>
    {
        { "RU", 1000017 },
        { "EN", 1000000 }, { "CN", 1000001 }, { "AR", 1000002 }, { "FR", 1000003 },
        { "DE", 1000009 }, { "ES", 1000010 }, { "IT", 1000011 }, { "JP", 1000012 },
        { "CNT", 1000013 }, { "KR", 1000014 }, { "NL", 1000015 }, { "PT", 1000016 },
        { "TR", 1000018 }, { "ID", 1000019 }, { "MS", 1000020 }, { "VI", 1000021 },
        { "TH", 1000022 }, { "FI", 1000023 }, { "PL", 1000026 }, { "HE", 1000027 }
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<int, string> IdToLanguage = LanguageToId
        .ToDictionary(kvp => kvp.Value, kvp => kvp.Key).ToFrozenDictionary();

    public static void LoadCache(FileServerService.FileServerServiceClient client)
    {
        var translations = new Dictionary<string, FrozenDictionary<uint, string>>();

        var files = client.GetAllFiles(new FileFilter { Extensions = { ".zjson" }, IncludeSubfolders = true });

        foreach (var file in files.Files)
        {
            var lang = Path.GetFileNameWithoutExtension(file.Path);

            var data = JsonConvert.DeserializeObject<Dictionary<uint, string>>(file.Data.ToStringUtf8())
                       ?? new Dictionary<uint, string>();

            translations[lang] = data.ToFrozenDictionary();
        }

        _cachedTranslations = translations.ToFrozenDictionary();
    }

    public static string GetTranslation(string languageCode, uint textId)
    {
        if (!_cachedTranslations.TryGetValue(languageCode, out var langDict))
            return $"INVALID_LANG_CODE:{languageCode}";

        return langDict.TryGetValue(textId, out var text) ? text : $"MISSING_TEXT_ID:{textId}";
    }

    public static string GetTranslation(int languageId, uint textId)
    {
        return !IdToLanguage.TryGetValue(languageId, out var langCode)
            ? $"INVALID_LANG_ID:{languageId}"
            : GetTranslation(langCode, textId);
    }
}