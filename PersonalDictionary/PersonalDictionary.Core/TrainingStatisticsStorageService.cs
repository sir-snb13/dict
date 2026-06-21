using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PersonalDictionary.Core;

public sealed class TrainingStatisticsStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string filePath;

    public TrainingStatisticsStorageService(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        filePath = Path.Combine(dataDirectory, "training-statistics.json");
    }

    public IReadOnlyList<TrainingStatistics> LoadStatistics()
    {
        if (!File.Exists(filePath))
        {
            return Array.Empty<TrainingStatistics>();
        }

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<TrainingStatistics>>(json, JsonOptions)
                ?.Where(item => item.TotalQuestions > 0)
                .ToList()
                ?? new List<TrainingStatistics>();
        }
        catch (JsonException)
        {
            return Array.Empty<TrainingStatistics>();
        }
    }

    public void SaveStatistics(IEnumerable<TrainingStatistics> statistics)
    {
        var results = statistics
            .Where(item => item.TotalQuestions > 0)
            .OrderBy(item => item.StartedAt)
            .ToList();
        var json = JsonSerializer.Serialize(results, JsonOptions);
        var temporaryFilePath = filePath + ".tmp";

        File.WriteAllText(temporaryFilePath, json);
        File.Move(temporaryFilePath, filePath, overwrite: true);
    }
}