using System.Text.Json.Serialization;

namespace PersonalDictionary.Core;

public sealed class TrainingStatistics
{
    public DateTime StartedAt { get; set; }

    public TrainingMode Mode { get; set; }

    public int TotalQuestions { get; set; }

    public int CorrectAnswers { get; set; }

    public int WrongAnswers { get; set; }

    [JsonIgnore]
    public double AccuracyPercent => TotalQuestions == 0
        ? 0
        : CorrectAnswers * 100.0 / TotalQuestions;
}