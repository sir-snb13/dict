namespace PersonalDictionary.Core;

public class TrainingQuestion
{
    public string QuestionText { get; set; } = "";

    public string CorrectAnswer { get; set; } = "";

    public WordEntry SourceWord { get; set; } = new();
}
