using System.Text.Encodings.Web;
using System.Text.Json;

namespace PersonalDictionary.Core;

public sealed class WordEntryStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string filePath;

    public WordEntryStorageService(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        filePath = Path.Combine(dataDirectory, "words.json");
    }

    public void MigrateFrom(string sourceFilePath)
    {
        if (File.Exists(filePath) || !File.Exists(sourceFilePath))
        {
            return;
        }

        File.Copy(sourceFilePath, filePath);
    }
    public IReadOnlyList<WordEntry> LoadEntries()
    {
        if (!File.Exists(filePath))
        {
            return Array.Empty<WordEntry>();
        }

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<WordEntry>>(json, JsonOptions)
                ?.Where(IsValidEntry)
                .ToList()
                ?? new List<WordEntry>();
        }
        catch (JsonException)
        {
            return Array.Empty<WordEntry>();
        }
    }

    public void SaveEntries(IEnumerable<WordEntry> entries)
    {
        var normalizedEntries = entries
            .Where(IsValidEntry)
            .Select(entry => new WordEntry
            {
                Word = entry.Word.Trim(),
                Translation = entry.Translation.Trim(),
                Category = entry.Category.Trim()
            })
            .ToList();

        var json = JsonSerializer.Serialize(normalizedEntries, JsonOptions);
        var temporaryFilePath = filePath + ".tmp";

        File.WriteAllText(temporaryFilePath, json);
        File.Move(temporaryFilePath, filePath, overwrite: true);
    }

    private static bool IsValidEntry(WordEntry entry)
    {
        return !string.IsNullOrWhiteSpace(entry.Word) &&
            !string.IsNullOrWhiteSpace(entry.Translation);
    }
}
