using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Application.Features.Exams.Preview;

/// <summary>
/// Builds the printable exam and answer-key models from a loaded exam graph. Both the web preview and the
/// PDF/Word exporters consume these models, so labelling and scrambling are identical everywhere.
/// </summary>
public static class ExamDocumentBuilder
{
    public static ExamPreviewDto BuildPreview(Exam exam, string universityName)
    {
        var questions = OrderedQuestions(exam)
            .Select((eq, index) =>
            {
                var layout = Layout(eq.Question);
                return new PreviewQuestionDto(
                    index + 1,
                    eq.Section,
                    eq.Question.Text,
                    eq.Question.Type,
                    eq.Points,
                    layout.Options,
                    layout.MatchItems,
                    AnswerLines(eq.Question.Type));
            })
            .ToList();

        return new ExamPreviewDto(BuildHeader(exam, universityName), questions);
    }

    public static ExamAnswerKeyDto BuildAnswerKey(Exam exam, string universityName)
    {
        var items = OrderedQuestions(exam)
            .Select((eq, index) => new AnswerKeyItemDto(
                index + 1,
                eq.Section,
                eq.Question.Text,
                eq.Question.Type,
                eq.Points,
                CorrectAnswer(eq.Question),
                eq.Question.Explanation))
            .ToList();

        return new ExamAnswerKeyDto(BuildHeader(exam, universityName), items);
    }

    private static IEnumerable<ExamQuestion> OrderedQuestions(Exam exam) =>
        exam.Questions.OrderBy(q => q.Order);

    private static ExamHeaderDto BuildHeader(Exam exam, string universityName) => new(
        universityName,
        exam.Course.Department.Name,
        exam.Course.Code,
        exam.Course.Name,
        exam.Title,
        exam.Description,
        exam.Instructions,
        exam.Type,
        exam.Status,
        exam.ExamDate,
        exam.DurationMinutes,
        exam.Questions.Sum(q => q.Points),
        exam.Questions.Count);

    private sealed record QuestionLayout(
        IReadOnlyList<PreviewItemDto> Options,
        IReadOnlyList<PreviewItemDto> MatchItems,
        IReadOnlyList<QuestionOption> DisplayOrder,
        IReadOnlyList<QuestionOption> MatchDisplayOrder);

    private static QuestionLayout Layout(Question question)
    {
        var options = question.Options.OrderBy(o => o.Order).ToList();

        switch (question.Type)
        {
            case QuestionType.MultipleChoice:
            case QuestionType.MultipleSelect:
            case QuestionType.TrueFalse:
                return new QuestionLayout(Label(options, o => o.Text, LetterUpper), [], options, []);

            case QuestionType.Matching:
                // Left column in authored order (1, 2, 3...), right column scrambled (a, b, c...).
                var right = Scramble(options, question.Id);
                return new QuestionLayout(
                    Label(options, o => o.Text, Number),
                    Label(right, o => o.MatchText ?? string.Empty, LetterLower),
                    options,
                    right);

            case QuestionType.Ordering:
                var scrambled = Scramble(options, question.Id);
                return new QuestionLayout(Label(scrambled, o => o.Text, LetterUpper), [], scrambled, []);

            default:
                return new QuestionLayout([], [], [], []);
        }
    }

    private static string CorrectAnswer(Question question)
    {
        var layout = Layout(question);

        return question.Type switch
        {
            QuestionType.MultipleChoice or QuestionType.TrueFalse =>
                string.Join(", ", layout.DisplayOrder
                    .Select((o, i) => (o, i))
                    .Where(x => x.o.IsCorrect)
                    .Select(x => $"{LetterUpper(x.i)}. {x.o.Text}")),

            QuestionType.MultipleSelect =>
                string.Join(", ", layout.DisplayOrder
                    .Select((o, i) => (o, i))
                    .Where(x => x.o.IsCorrect)
                    .Select(x => LetterUpper(x.i))),

            QuestionType.Matching =>
                string.Join(", ", layout.DisplayOrder.Select((o, i) =>
                    $"{Number(i)} → {LetterLower(IndexOf(layout.MatchDisplayOrder, o))}")),

            QuestionType.Ordering =>
                string.Join(" → ", question.Options.OrderBy(o => o.Order)
                    .Select(o => LetterUpper(IndexOf(layout.DisplayOrder, o)))),

            QuestionType.Essay => string.IsNullOrWhiteSpace(question.ExpectedAnswer)
                ? "Open answer — grade according to the marking rubric."
                : question.ExpectedAnswer!,

            _ => question.ExpectedAnswer ?? string.Empty
        };
    }

    private static int AnswerLines(QuestionType type) => type switch
    {
        QuestionType.ShortAnswer => 3,
        QuestionType.Essay => 10,
        _ => 0
    };

    /// <summary>Deterministic scramble seeded by the question id, stable across preview, exports and answer key.</summary>
    private static List<QuestionOption> Scramble(List<QuestionOption> options, Guid seedSource)
    {
        var result = options.ToList();
        var random = new Random(BitConverter.ToInt32(seedSource.ToByteArray(), 0));
        for (var i = result.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        // Avoid printing the answer when the scramble happens to keep the original order.
        if (result.Count > 1 && result.SequenceEqual(options))
            (result[0], result[1]) = (result[1], result[0]);

        return result;
    }

    private static int IndexOf(IReadOnlyList<QuestionOption> list, QuestionOption option)
    {
        for (var i = 0; i < list.Count; i++)
            if (ReferenceEquals(list[i], option) || list[i].Id == option.Id)
                return i;
        return -1;
    }

    private static IReadOnlyList<PreviewItemDto> Label(
        IEnumerable<QuestionOption> options, Func<QuestionOption, string> text, Func<int, string> label) =>
        options.Select((o, i) => new PreviewItemDto(label(i), text(o))).ToList();

    private static string LetterUpper(int index) => ((char)('A' + index)).ToString();
    private static string LetterLower(int index) => ((char)('a' + index)).ToString();
    private static string Number(int index) => (index + 1).ToString();
}
