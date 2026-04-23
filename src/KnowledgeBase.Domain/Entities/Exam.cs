using KnowledgeBase.Domain.Common;
using KnowledgeBase.Domain.Enums;

namespace KnowledgeBase.Domain.Entities;

public class Exam : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public long CreatorId { get; set; }
    public User? Creator { get; set; }
    public int TimeLimitMinutes { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public ExamStatus Status { get; set; } = ExamStatus.Draft;
    public int TotalScore { get; set; }
    public int PassScore { get; set; }

    public ICollection<ExamQuestion> Questions { get; set; } = new List<ExamQuestion>();
    public ICollection<ExamRecord> Records { get; set; } = new List<ExamRecord>();
}

public class ExamQuestion : BaseEntity
{
    public long ExamId { get; set; }
    public Exam? Exam { get; set; }
    public ExamQuestionType QuestionType { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? Options { get; set; } // JSON: ["A. xxx", "B. xxx", ...]
    public string CorrectAnswer { get; set; } = string.Empty;
    public int Score { get; set; }
    public int SortOrder { get; set; }
}

public class ExamRecord : BaseEntity
{
    public long ExamId { get; set; }
    public Exam? Exam { get; set; }
    public long UserId { get; set; }
    public User? User { get; set; }
    public string? Answers { get; set; } // JSON: { "questionId": "answer" }
    public int TotalScore { get; set; }
    public bool IsPassed { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int TimeSpentSeconds { get; set; }
}
