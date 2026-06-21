namespace PersonalDictionary.Core;

public class TrainingSession
{
    private readonly Random random = new();

    public int TotalQuestions { get; private set; }

    public int CorrectAnswers { get; private set; }

    public int WrongAnswers { get; private set; }

    public TrainingQuestion CreateQuestion(IReadOnlyList<WordEntry> words, TrainingMode mode)
    {
        if (words.Count == 0)
        {
            throw new InvalidOperationException("The word list is empty.");
        }

        var sourceWord = words[random.Next(words.Count)];

        return mode == TrainingMode.WordToTranslation
            ? new TrainingQuestion
            {
                QuestionText = sourceWord.Word,
                CorrectAnswer = sourceWord.Translation,
                SourceWord = sourceWord
            }
            : new TrainingQuestion
            {
                QuestionText = sourceWord.Translation,
                CorrectAnswer = sourceWord.Word,
                SourceWord = sourceWord
            };
    }

    public bool CheckAnswer(TrainingQuestion question, string answer)
    {
        var isCorrect = string.Equals(
            answer.Trim(),
            question.CorrectAnswer.Trim(),
            StringComparison.OrdinalIgnoreCase);

        TotalQuestions++;

        if (isCorrect)
        {
            CorrectAnswers++;
        }
        else
        {
            WrongAnswers++;
        }

        return isCorrect;
    }

    public void Reset()
    {
        TotalQuestions = 0;
        CorrectAnswers = 0;
        WrongAnswers = 0;
    }
}
