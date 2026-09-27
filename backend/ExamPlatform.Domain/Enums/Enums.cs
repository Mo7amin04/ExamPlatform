namespace ExamPlatform.Domain.Enums;

public enum QuestionType
{
    MultipleChoice = 1,
    TrueFalse = 2,
    MultipleSelect = 3,
    ShortAnswer = 4,
    Essay = 5,
    FillBlank = 6,
    Matching = 7,
    Ordering = 8
}

public enum Difficulty
{
    Easy = 1,
    Medium = 2,
    Hard = 3
}

public enum BloomLevel
{
    Remember = 1,
    Understand = 2,
    Apply = 3,
    Analyze = 4,
    Evaluate = 5,
    Create = 6
}

public enum QuestionStatus
{
    Draft = 1,
    Approved = 2,
    Archived = 3
}

public enum QuestionSource
{
    Manual = 1,
    AI = 2
}

public enum ExamType
{
    Quiz = 1,
    Midterm = 2,
    Final = 3,
    Assignment = 4,
    Practice = 5
}

public enum ExamStatus
{
    Draft = 1,
    Ready = 2,
    Published = 3,
    Archived = 4
}

public enum AIOperation
{
    Generate = 1,
    Improve = 2
}

public enum AIGenerationStatus
{
    Succeeded = 1,
    Failed = 2
}
