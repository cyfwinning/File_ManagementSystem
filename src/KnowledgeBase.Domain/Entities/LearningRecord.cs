using KnowledgeBase.Domain.Common;
using KnowledgeBase.Domain.Enums;

namespace KnowledgeBase.Domain.Entities;

public class LearningRecord : BaseEntity
{
    public long UserId { get; set; }
    public User? User { get; set; }
    public long DocumentId { get; set; }
    public Document? Document { get; set; }
    public long? RecommendationId { get; set; }
    public Recommendation? Recommendation { get; set; }
    public LearningStatus Status { get; set; } = LearningStatus.NotStarted;
    public int ProgressPercent { get; set; }
    public int TotalSeconds { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastAccessAt { get; set; }
}
