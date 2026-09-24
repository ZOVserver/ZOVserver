using System.Text;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZOVserver.Shared.Contracts.Proto;

namespace ZOVserver.Shared.Localization;

public class LocalizationManager(FileServerService.FileServerServiceClient client)
{
    private static readonly Dictionary<string, string> Languages = new()
    {
        { "EN", "en" },
        { "CN", "zh-CN" },
        { "AR", "ar" },
        { "FR", "fr" },
        { "DE", "de" },
        { "ES", "es" },
        { "IT", "it" },
        { "JP", "ja" },
        { "CNT", "zh-TW" },
        { "KR", "ko" },
        { "NL", "nl" },
        { "PT", "pt" },
        { "RU", "ru" },
        { "TR", "tr" },
        { "ID", "id" },
        { "MS", "ms" },
        { "VI", "vi" },
        { "TH", "th" },
        { "FI", "fi" },
        { "PL", "pl" },
        { "HE", "he" }
    };

    public async Task AddTranslationAsync(string englishText)
    {
        foreach (var lang in Languages)
        {
            var filePath = $"Localizations/{lang.Key}.zjson";
            var translations = await LoadTranslations(filePath);

            var newId = translations.Count != 0 ? translations.Keys.Max() + 1 : 0;

            var translatedText = lang.Key == "EN"
                ? englishText
                : await TranslateToAsync(lang.Value, englishText);

            translations[newId] = translatedText;
            await SaveTranslations(filePath, translations);
        }
    }

    private static async Task<string> TranslateToAsync(string targetLanguageCode, string text)
    {
        using var client = new HttpClient();

        var response = await client.GetAsync(
            $"https://translate.googleapis.com/translate_a/single?" +
            $"client=gtx&sl=auto&tl={targetLanguageCode}&dt=t&q={Uri.EscapeDataString(text)}");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();

        try
        {
            var jsonArray = JArray.Parse(result);

            if (jsonArray.Count > 0 && jsonArray[0] is JArray sentencesArray)
            {
                var translatedParts = new List<string>();

                foreach (var sentence in sentencesArray)
                    if (sentence is JArray { Count: > 0 } sentenceParts)
                        translatedParts.Add(sentenceParts[0].Value<string>()!);

                return string.Join("", translatedParts);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing translation response: {ex.Message}");
            Console.WriteLine($"Raw response: {result}");
        }

        return text;
    }

    private async Task<Dictionary<uint, string>> LoadTranslations(string filePath)
    {
        try
        {
            var file = await client.GetFileAsync(new FileRequest { Path = filePath });

            var text = Encoding.UTF8.GetString(file.Data.ToArray());

            return JsonConvert.DeserializeObject<Dictionary<uint, string>>(text) ?? new Dictionary<uint, string>();
        }
        catch
        {
            return new Dictionary<uint, string>();
        }
    }

    private async Task SaveTranslations(string filePath, Dictionary<uint, string> translations)
    {
        var b = JsonConvert.SerializeObject(translations, Formatting.Indented);
        await client.UploadFileAsync(
            new UploadRequest { TargetPath = filePath, ChunkData = ByteString.CopyFromUtf8(b) });
    }
}