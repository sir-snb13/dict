using System.Text.Json;

namespace PersonalDictionary.Core;

public class TranslationService
{
    private static readonly HttpClient HttpClient = new();

    public static WordLanguage DetectLanguage(string text)
    {
        var hasRussianLetters = text.Any(character =>
            character is >= 'А' and <= 'я' or 'Ё' or 'ё');
        var hasEnglishLetters = text.Any(character =>
            character is >= 'A' and <= 'Z' or >= 'a' and <= 'z');

        return (hasRussianLetters, hasEnglishLetters) switch
        {
            (true, false) => WordLanguage.Russian,
            (false, true) => WordLanguage.English,
            _ => throw new ArgumentException(
                "Text must contain letters from exactly one supported language.",
                nameof(text))
        };
    }

    public static WordEntry CreateNormalizedWordEntry(
        string firstText,
        string secondText,
        string category)
    {
        var normalizedFirstText = firstText.Trim();
        var normalizedSecondText = secondText.Trim();
        var firstLanguage = DetectLanguage(normalizedFirstText);
        var secondLanguage = DetectLanguage(normalizedSecondText);

        if (firstLanguage == secondLanguage)
        {
            throw new ArgumentException("Word and translation must use different languages.");
        }

        return firstLanguage == WordLanguage.Russian
            ? new WordEntry
            {
                Word = normalizedFirstText,
                Translation = normalizedSecondText,
                Category = category
            }
            : new WordEntry
            {
                Word = normalizedSecondText,
                Translation = normalizedFirstText,
                Category = category
            };
    }
    public async Task<WordEntry> CreateWordEntryAsync(string text, string category)
    {
        var normalizedText = text.Trim();
        var language = DetectLanguage(normalizedText);

        if (language == WordLanguage.Russian)
        {
            return new WordEntry
            {
                Word = normalizedText,
                Translation = await TranslateAsync(normalizedText, "ru", "en"),
                Category = category
            };
        }

        return new WordEntry
        {
            Word = await TranslateAsync(normalizedText, "en", "ru"),
            Translation = normalizedText,
            Category = category
        };
    }

    public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        var encodedText = Uri.EscapeDataString(text);
        var encodedLangPair = Uri.EscapeDataString($"{sourceLanguage}|{targetLanguage}");
        var requestUri = $"https://api.mymemory.translated.net/get?q={encodedText}&langpair={encodedLangPair}";

        using var response = await HttpClient.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("responseData", out var responseData) ||
            !responseData.TryGetProperty("translatedText", out var translatedTextElement))
        {
            throw new InvalidOperationException("Translation was not found in API response.");
        }

        var translatedText = translatedTextElement.GetString();

        if (string.IsNullOrWhiteSpace(translatedText))
        {
            throw new InvalidOperationException("Translation is empty.");
        }

        return translatedText;
    }
}
