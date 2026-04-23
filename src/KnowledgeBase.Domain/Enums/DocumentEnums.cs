namespace KnowledgeBase.Domain.Enums;

public enum DocumentStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2,
    PendingApproval = 3
}

public enum DocumentEditMode
{
    RichText = 0,
    Markdown = 1
}

public enum AttachmentType
{
    Document = 0,
    Image = 1,
    Audio = 2,
    Video = 3,
    Other = 4
}

public enum UrgencyLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}

public enum LearningStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2
}

public enum ExamQuestionType
{
    SingleChoice = 0,
    MultipleChoice = 1,
    FillBlank = 2
}

public enum ExamStatus
{
    Draft = 0,
    NotStarted = 1,
    InProgress = 2,
    Ended = 3
}

public enum PermissionAction
{
    View = 0,
    Edit = 1,
    Delete = 2,
    Share = 3,
    Download = 4,
    Export = 5
}

public enum DatabaseType
{
    SQLite = 0,
    MySQL = 1
}
